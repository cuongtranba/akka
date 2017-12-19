using System;
using Akka.Actor;
using Akka.Event;
using Example.Messages;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Example.Actors
{
    public class IotSupervisor : UntypedActor
    {
        public ILoggingAdapter Log { get; } = Context.GetLogger();

        protected override void PreStart() => Log.Info("IoT Application started");
        protected override void PostStop() => Log.Info("IoT Application stopped");


        public static Props Props() => Akka.Actor.Props.Create<IotSupervisor>();

        protected override void OnReceive(object message)
        {

        }
    }

    public class Device : UntypedActor
    {
        private double? _lastTemperatureReading = null;
        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        protected string GroupId { get; }
        protected string DeviceId { get; }
        public static Props Props(string groupId, string deviceId) => Akka.Actor.Props.Create(() => new Device(groupId, deviceId));

        public Device(string groupId, string deviceId)
        {
            GroupId = groupId;
            DeviceId = deviceId;
        }

        protected override void PreStart() => Log.Info($"Device actor {GroupId}-{DeviceId} started");
        protected override void PostStop() => Log.Info($"Device actor {GroupId}-{DeviceId} stopped");

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestTrackDevice req when req.GroupId.Equals(GroupId) && req.DeviceId.Equals(DeviceId):
                    Sender.Tell(DeviceRegistered.Instance);
                    break;
                case RequestTrackDevice req:
                    Log.Warning($"Ignoring TrackDevice request for {req.GroupId}-{req.DeviceId}.This actor is responsible for {GroupId}-{DeviceId}.");
                    break;
                case RecordTemperature rec:
                    Log.Info($"Recorded temperature reading {rec.Value} with {rec.RequestId}");
                    _lastTemperatureReading = rec.Value;
                    Sender.Tell(new TemperatureRecorded(rec.RequestId));
                    break;
                case ReadTemperature read:
                    Sender.Tell(new RespondTemperature(read.RequestId, _lastTemperatureReading));
                    break;
            }
        }
    }

    public class DeviceGroup : UntypedActor
    {
        private Dictionary<string, IActorRef> deviceIdToActor = new Dictionary<string, IActorRef>();
        private Dictionary<IActorRef, string> actorToDeviceId = new Dictionary<IActorRef, string>();


        protected override void PreStart() => Log.Info($"Device group {GroupId} started");
        protected override void PostStop() => Log.Info($"Device group {GroupId} stopped");

        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        protected string GroupId { get; }

        public static Props Props(string groupId) => Akka.Actor.Props.Create(() => new DeviceGroup(groupId));

        public DeviceGroup(string groupId)
        {
            GroupId = groupId;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestTrackDevice trackMsg when trackMsg.GroupId.Equals(GroupId):
                    if (deviceIdToActor.TryGetValue(trackMsg.DeviceId, out var actorRef))
                    {
                        actorRef.Forward(trackMsg);
                    }
                    else
                    {
                        Log.Info($"Creating device actor for {trackMsg.DeviceId}");
                        var deviceActor = Context.ActorOf(Device.Props(trackMsg.GroupId, trackMsg.DeviceId), $"device-{trackMsg.DeviceId}");
                        Context.Watch(deviceActor);
                        actorToDeviceId.Add(deviceActor, trackMsg.DeviceId);
                        deviceIdToActor.Add(trackMsg.DeviceId, deviceActor);
                        deviceActor.Forward(trackMsg);
                    }
                    break;
                case RequestTrackDevice trackMsg:
                    Log.Warning($"Ignoring TrackDevice request for {trackMsg.GroupId}. This actor is responsible for {GroupId}.");
                    break;
                case RequestDeviceList deviceList:
                    Sender.Tell(new ReplyDeviceList(deviceList.RequestId, new HashSet<string>(deviceIdToActor.Keys)));
                    break;
                case Terminated t:
                    var deviceId = actorToDeviceId[t.ActorRef];
                    Log.Info($"Device actor for {deviceId} has been terminated");
                    actorToDeviceId.Remove(t.ActorRef);
                    deviceIdToActor.Remove(deviceId);
                    break;
            }
        }
    }

    public class DeviceManage : UntypedActor
    {
        public static Props Props(string groupId) => Akka.Actor.Props.Create<DeviceManage>();

        private Dictionary<string, IActorRef> groupIdToActor = new Dictionary<string, IActorRef>();
        private Dictionary<IActorRef, string> actorToGroupId = new Dictionary<IActorRef, string>();

        protected override void PreStart() => Log.Info("DeviceManager started");
        protected override void PostStop() => Log.Info("DeviceManager stopped");

        protected ILoggingAdapter Log { get; } = Context.GetLogger();

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestTrackDevice trackMsg:
                    if (groupIdToActor.TryGetValue(trackMsg.GroupId, out var actorRef))
                    {
                        actorRef.Forward(trackMsg);
                    }
                    else
                    {
                        Log.Info($"Creating device group actor for {trackMsg.GroupId}");
                        var groupActor = Context.ActorOf(DeviceGroup.Props(trackMsg.GroupId), $"group-{trackMsg.GroupId}");
                        Context.Watch(groupActor);
                        groupActor.Forward(trackMsg);
                        groupIdToActor.Add(trackMsg.GroupId, groupActor);
                        actorToGroupId.Add(groupActor, trackMsg.GroupId);
                    }
                    break;
                case Terminated t:
                    var groupId = actorToGroupId[t.ActorRef];
                    Log.Info($"Device group actor for {groupId} has been terminated");
                    actorToGroupId.Remove(t.ActorRef);
                    groupIdToActor.Remove(groupId);
                    break;
            }
        }
    }
    public class DeviceGroupQuery : UntypedActor
    {
        private readonly ICancelable _queryTimeoutTimer;
        public static Props Props(Dictionary<IActorRef, string> actorToDeviceId, long requestId, IActorRef requester, TimeSpan timeout) =>
            Akka.Actor.Props.Create(() => new DeviceGroupQuery(actorToDeviceId, requestId, requester, timeout));

        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        public Dictionary<IActorRef, string> ActorToDeviceId { get; }
        public long RequestId { get; }
        public IActorRef Requester { get; }
        public TimeSpan Timeout { get; }

        public DeviceGroupQuery(Dictionary<IActorRef, string> actorToDeviceId, long requestId, IActorRef requester, TimeSpan timeout)
        {
            ActorToDeviceId = actorToDeviceId;
            RequestId = requestId;
            Requester = requester;
            Timeout = timeout;

            _queryTimeoutTimer = Context.System.Scheduler.ScheduleTellOnceCancelable(timeout, Self, CollectionTimeout.Instance, Self);
        }

        protected override void PreStart()
        {
            foreach (var deviceActor in ActorToDeviceId.Keys)
            {
                Context.Watch(deviceActor);
                deviceActor.Tell(new ReadTemperature(0));
            }
        }

        protected override void PostStop()
        {
            _queryTimeoutTimer.Cancel();
        }


        protected override void OnReceive(object message)
        {

        }


    }


}
