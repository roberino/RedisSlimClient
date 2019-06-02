namespace RedisSlimClient.Telemetry
{
    class TelemetricContext
    {
        readonly ITelemetryWriter _writer;
        readonly TelemetryEvent _operation;

        public TelemetricContext(ITelemetryWriter writer, TelemetryEvent operation)
        {
            _writer = writer;
            _operation = operation;
        }

        public void Write(string eventName)
        {
            var ev = _operation.CreateChild(eventName);
            _writer.Write(ev);
        }
    }
}