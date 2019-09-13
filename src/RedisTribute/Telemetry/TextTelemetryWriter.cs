using System;

namespace RedisTribute.Telemetry
{
    public class TextTelemetryWriter : ITelemetryWriter
    {
        readonly Action<string> _writeMethod;

        public TextTelemetryWriter(Action<string> writeMethod, Severity severity = Severity.Warn | Severity.Error)
        {
            _writeMethod = writeMethod;
            Severity = severity;
        }

        public bool Enabled => Severity != Severity.None;

        public Severity Severity { get; }

        public void Write(TelemetryEvent telemetryEvent)
        {
            if (Enabled && Severity.HasFlag(telemetryEvent.Severity))
            {
                _writeMethod($"{telemetryEvent.Timestamp:s}: {telemetryEvent.OperationId} {telemetryEvent.Name} {telemetryEvent.Action} [{telemetryEvent.Elapsed}] {telemetryEvent.Data}");

                if (telemetryEvent.Exception != null)
                {
                    _writeMethod(telemetryEvent.Exception.Message);
                }
            }
        }
    }
}