namespace RedisTribute.Telemetry
{
    class NullWriter : ITelemetryWriter
    {
        NullWriter() { }

        public static readonly ITelemetryWriter Instance = new NullWriter();

        public bool Enabled => false;

        public Severity Severity => Severity.None;

        public TelemetryCategory Category => TelemetryCategory.Internal;

        public void Write(TelemetryEvent telemetryEvent)
        {
        }

        public void Flush()
        {
        }
    }
}