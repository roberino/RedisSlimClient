namespace RedisSlimClient.Telemetry
{
    public interface ITelemetryWriter
    {
        bool Enabled { get; }
        void Write(TelemetryEvent telemetryEvent);
    }
}