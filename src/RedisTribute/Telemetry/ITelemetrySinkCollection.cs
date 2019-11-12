namespace RedisTribute.Telemetry
{
    public interface ITelemetrySinkCollection
    {
        void Add(ITelemetryWriter writer);
    }
}