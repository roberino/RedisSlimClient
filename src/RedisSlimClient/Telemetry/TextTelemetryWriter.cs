using System;

namespace RedisSlimClient.Telemetry
{
    public class TextTelemetryWriter : ITelemetryWriter
    {
        private readonly Action<string> writeMethod;

        public TextTelemetryWriter(Action<string> writeMethod)
        {
            this.writeMethod = writeMethod;
        }

        public void Write(TelemetryEvent telemetryEvent)
        {
            writeMethod($"{telemetryEvent.Timestamp}: {telemetryEvent.OperationId} {telemetryEvent.Name} {telemetryEvent.Action} [{telemetryEvent.Elapsed}] data={telemetryEvent.Data}");

            if (telemetryEvent.Exception != null)
            {
                writeMethod(telemetryEvent.Exception.Message);
            }
        }
    }
}