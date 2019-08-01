using System;

namespace RedisSlimClient.Telemetry
{
    public class TextTelemetryWriter : ITelemetryWriter
    {
        readonly Action<string> _writeMethod;
        readonly Severity _severity;

        public TextTelemetryWriter(Action<string> writeMethod, Severity severity = Severity.Warn | Severity.Error)
        {
            _writeMethod = writeMethod;
            _severity = severity;
        }

        public bool Enabled => _severity != Severity.None;

        public Severity Severity => _severity;

        public void Write(TelemetryEvent telemetryEvent)
        {
            if (Enabled)
            {
                if (_severity.HasFlag(telemetryEvent.Severity))
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
}