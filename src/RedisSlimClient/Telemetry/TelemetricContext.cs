using System.Collections.Generic;

namespace RedisSlimClient.Telemetry
{
    class TelemetricContext
    {
        readonly ITelemetryWriter _writer;
        readonly TelemetryEvent _operation;

        public TelemetricContext(ITelemetryWriter writer, TelemetryEvent operation, IDictionary<string, object> dimensions)
        {
            _writer = writer;
            _operation = operation;

            Dimensions = dimensions;
        }

        public IDictionary<string, object> Dimensions { get; }

        public void Write(string eventName)
        {
            var ev = _operation.CreateChild(eventName);
            _writer.Write(ev);
        }
    }
}