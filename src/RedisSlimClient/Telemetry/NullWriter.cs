namespace RedisSlimClient.Telemetry
{
    class NullWriter : ITelemetryWriter
    {
        NullWriter() { }

        public static readonly ITelemetryWriter Instance = new NullWriter();

        public bool Enabled => false;

        public void Write(TelemetryEvent telemetryEvent)
        {
        }
    }
}
