namespace RedisTribute.Telemetry
{
    public interface ITelemetryWriter
    {
        TelemetryCategory Category { get; }
        Severity Severity { get; }
        bool Enabled { get; }
        void Write(TelemetryEvent telemetryEvent);
        void Flush();
    }
}