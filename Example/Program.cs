using Akka.Actor;
using Akka.Event;
using Example.Actors;
using Example.Messages;
using System;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Xunit;

namespace Example
{
    public class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("iot-system"))
            {
                //var supervior = system.ActorOf(Props.Create<IotSupervisor>(), "iot-supervisor");
                ////var deviceActor = system.ActorOf(Device.Props("group", "device"));
                //var groupActor = system.ActorOf(DeviceGroup.Props("group"));

                ////deviceActor.Tell(new RecordTemperature(1,23),supervior);
                ////deviceActor.Tell(new ReadTemperature(requestId: 2), supervior);

                ////deviceActor.Tell(new RecordTemperature(requestId: 3, value: 55.0), supervior);
                ////deviceActor.Tell(new ReadTemperature(requestId: 4), supervior);

                ////deviceActor.Tell(new RequestTrackDevice("group", "device"), supervior);

                //groupActor.Tell(new RequestTrackDevice("group", "device1"), supervior);

                //var supervior2 = system.ActorOf(Props.Create<IotSupervisor>(), "iot-supervisor2");
                //groupActor.Tell(new RequestTrackDevice("group", "device2"), supervior2);

                //Console.ReadLine();
            }
        }
    }

    public class DeviceGroupSpec : TestKit
    {
        [Fact]
        public void Device_actor_must_reply_to_registration_requests()
        {
            var probe = CreateTestProbe();
            var deviceActor = Sys.ActorOf(Device.Props("group", "device"));

            deviceActor.Tell(new RequestTrackDevice("group", "device"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            probe.LastSender.Should().Be(deviceActor);
        }

        [Fact]
        public void Device_actor_must_ignore_wrong_registration_requests()
        {
            var probe = CreateTestProbe();
            var deviceActor = Sys.ActorOf(Device.Props("group", "device"));

            deviceActor.Tell(new RequestTrackDevice("wrongGroup", "device"), probe.Ref);
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));

            deviceActor.Tell(new RequestTrackDevice("group", "Wrongdevice"), probe.Ref);
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }
        [Fact]
        public void DeviceGroup_actor_must_be_able_to_register_a_device_actor()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceGroup.Props("group"));
            groupActor.Tell(new RequestTrackDevice("group", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();

            var deviceActor1 = probe.LastSender;

            groupActor.Tell(new RequestTrackDevice("group", "device2"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor2 = probe.LastSender;
            deviceActor1.Should().NotBe(deviceActor2);

            // Check that the device actors are working
            deviceActor1.Tell(new RecordTemperature(requestId: 0, value: 1.0), probe.Ref);
            probe.ExpectMsg<TemperatureRecorded>(s => s.RequestId == 0);
            deviceActor2.Tell(new RecordTemperature(requestId: 1, value: 2.0), probe.Ref);
            probe.ExpectMsg<TemperatureRecorded>(s => s.RequestId == 1);
        }

        [Fact]
        public void DeviceGroup_actor_must_ignore_requests_for_wrong_groupId()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceGroup.Props("group"));

            groupActor.Tell(new RequestTrackDevice("wrongGroup", "device1"), probe.Ref);
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [Fact]
        public void DeviceGroup_actor_must_return_same_actor_for_same_deviceId()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceGroup.Props("group"));

            groupActor.Tell(new RequestTrackDevice("group", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor1 = probe.LastSender;

            groupActor.Tell(new RequestTrackDevice("group", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor2 = probe.LastSender;

            deviceActor1.Should().Be(deviceActor2);
        }
        [Fact]
        public void DeviceGroup_actor_must_be_able_to_list_active_devices()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceGroup.Props("group"));

            groupActor.Tell(new RequestTrackDevice("group", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();

            groupActor.Tell(new RequestTrackDevice("group", "device2"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();

            groupActor.Tell(new RequestDeviceList(requestId: 0), probe.Ref);
            probe.ExpectMsg<ReplyDeviceList>(s => s.RequestId == 0
                                                  && s.Ids.Contains("device1")
                                                  && s.Ids.Contains("device2"));
        }

        [Fact]
        public void DeviceGroup_actor_must_be_able_to_list_active_devices_after_one_shuts_down()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceGroup.Props("group"));

            groupActor.Tell(new RequestTrackDevice("group", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var toShutDown = probe.LastSender;

            groupActor.Tell(new RequestTrackDevice("group", "device2"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();

            groupActor.Tell(new RequestDeviceList(requestId: 0), probe.Ref);
            probe.ExpectMsg<ReplyDeviceList>(s => s.RequestId == 0
                                                  && s.Ids.Contains("device1")
                                                  && s.Ids.Contains("device2"));

            probe.Watch(toShutDown);
            toShutDown.Tell(PoisonPill.Instance);
            probe.ExpectTerminated(toShutDown);

            // using awaitAssert to retry because it might take longer for the groupActor
            // to see the Terminated, that order is undefined
            probe.AwaitAssert(() =>
            {
                groupActor.Tell(new RequestDeviceList(requestId: 1), probe.Ref);
                probe.ExpectMsg<ReplyDeviceList>(s => s.RequestId == 1 && s.Ids.Contains("device2"));
            });
        }


    }
}
