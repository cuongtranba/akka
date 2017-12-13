namespace Example.Messages
{
    public sealed class RecordTemperature
    {
        public RecordTemperature(long requestId, double value)
        {
            RequestId = requestId;
            Value = value;
        }

        public long RequestId { get; }
        public double Value { get; }
    }

    public sealed class TemperatureRecorded
    {
        public TemperatureRecorded(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }
}
