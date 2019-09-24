using RedisTribute.Telemetry;

namespace RedisTribute.Benchmarks
{
    class NullTelemetry : ITelemetryWriter
    {
        private NullTelemetry() { }

        public static readonly ITelemetryWriter Instance = new NullTelemetry();

        public Severity Severity => Severity.None;

        public TelemetryCategory Category => TelemetryCategory.None;

        public bool Enabled => false;

        public void Write(TelemetryEvent telemetryEvent)
        {
        }
    }
}
