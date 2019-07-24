using System;

namespace RedisSlimClient.Telemetry
{
    public class TextTelemetryWriter : ITelemetryWriter
    {
        readonly Action<string> _writeMethod;
        readonly Severity _severity;

        public TextTelemetryWriter(Action<string> writeMethod, Severity severity = Severity.Info)
        {
            _writeMethod = writeMethod;
            _severity = severity;
        }

        public bool Enabled => true;

        public void Write(TelemetryEvent telemetryEvent)
        {
            if (_severity.HasFlag(telemetryEvent.Severity))
            {
                _writeMethod($"{telemetryEvent.Timestamp}: {telemetryEvent.OperationId} {telemetryEvent.Name} {telemetryEvent.Action} [{telemetryEvent.Elapsed}] data={telemetryEvent.Data}");

                if (telemetryEvent.Exception != null)
                {
                    _writeMethod(telemetryEvent.Exception.Message);
                }
            }
        }
    }
}