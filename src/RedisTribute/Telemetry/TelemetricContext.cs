using System;
using System.Collections.Generic;

namespace RedisTribute.Telemetry
{
    class TelemetricContext : IDisposable
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

        public void Dispose()
        {
            _operation.Dispose();
        }

        public void Write(string eventName)
        {
            var ev = TelemetryEventFactory.Instance.Create(eventName, _operation.OperationId);

            _writer.Write(ev);
        }
    }
}