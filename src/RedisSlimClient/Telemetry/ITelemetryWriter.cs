namespace RedisSlimClient.Telemetry
{
    public interface ITelemetryWriter
    {
        Severity Severity { get; }
        bool Enabled { get; }
        void Write(TelemetryEvent telemetryEvent);
    }
}