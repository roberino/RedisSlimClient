namespace RedisSlimClient.Telemetry
{
    class NullWriter : ITelemetryWriter
    {
        public void Write(TelemetryEvent telemetryEvent)
        {
        }
    }
}
