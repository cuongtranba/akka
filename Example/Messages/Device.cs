using System.Collections.Generic;

namespace Example.Messages
{
    public sealed class RequestTrackDevice
    {
        public RequestTrackDevice(string groupId, string deviceId)
        {
            GroupId = groupId;
            DeviceId = deviceId;
        }
        public string GroupId { get; }
        public string DeviceId { get; }
    }

    public sealed class DeviceRegistered
    {
        public static DeviceRegistered Instance { get; } = new DeviceRegistered();
        private DeviceRegistered() { }
    }
    public sealed class RequestDeviceList
    {
        public RequestDeviceList(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class ReplyDeviceList
    {
        public ReplyDeviceList(long requestId, ISet<string> ids)
        {
            RequestId = requestId;
            Ids = ids;
        }

        public long RequestId { get; }
        public ISet<string> Ids { get; }
    }


}
