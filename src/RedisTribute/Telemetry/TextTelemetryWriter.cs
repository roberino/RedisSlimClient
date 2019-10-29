using System;

namespace RedisTribute.Telemetry
{
    public class TextTelemetryWriter : ITelemetryWriter
    {
        readonly Action<string> _writeMethod;

        public TextTelemetryWriter(Action<string> writeMethod, Severity severity = Severity.Warn | Severity.Error, TelemetryCategory category = TelemetryCategory.Health | TelemetryCategory.Internal | TelemetryCategory.Request)
        {
            _writeMethod = writeMethod;
            Severity = severity;
            Category = category;
        }

        public bool Enabled => Severity != Severity.None;

        public Severity Severity { get; }
        public TelemetryCategory Category { get; }

        public void Flush()
        {
        }

        public void Write(TelemetryEvent telemetryEvent)
        {
            if (Enabled && Severity.HasFlag(telemetryEvent.Severity) && Category.HasFlag(telemetryEvent.Category))
            {
                _writeMethod($"{telemetryEvent.Timestamp:s}: {telemetryEvent.OperationId} {telemetryEvent.Category} {telemetryEvent.Sequence} {telemetryEvent.Name} [{telemetryEvent.Elapsed}] {telemetryEvent.Data}");

                if (telemetryEvent.Exception != null)
                {
                    _writeMethod(telemetryEvent.Exception.Message);
                }

                foreach(var dim in telemetryEvent.Dimensions)
                {
                    _writeMethod($"\t-{dim.Key}={dim.Value}");
                }
            }
        }
    }
}