namespace RedisSlimClient.Telemetry
{
    public interface ITelemetryWriter
    {
        void Write(TelemetryEvent telemetryEvent);
    }
}