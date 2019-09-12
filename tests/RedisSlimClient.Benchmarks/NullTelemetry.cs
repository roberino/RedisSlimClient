using RedisSlimClient.Telemetry;

namespace RedisSlimClient.Benchmarks
{
    class NullTelemetry : ITelemetryWriter
    {
        private NullTelemetry() { }

        public static readonly ITelemetryWriter Instance = new NullTelemetry();

        public Severity Severity => Severity.None;

        public bool Enabled => false;

        public void Write(TelemetryEvent telemetryEvent)
        {
        }
    }
}
