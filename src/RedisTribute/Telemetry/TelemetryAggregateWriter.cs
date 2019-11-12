using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedisTribute.Telemetry
{
    class TelemetryAggregateWriter : ITelemetrySinkCollection, ITelemetryWriter, IEnumerable<ITelemetryWriter>
    {
        readonly IList<ITelemetryWriter> _sinks;

        public TelemetryAggregateWriter()
        {
            _sinks = new List<ITelemetryWriter>();
        }

        public TelemetryCategory Category { get; private set; }
        public Severity Severity { get; private set; }
        public bool Enabled => _sinks.Any(x => x.Enabled);

        public void Add(ITelemetryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            _sinks.Add(writer);

            Severity |= writer.Severity;
            Category |= writer.Category;
        }

        public void Flush()
        {
            foreach(var sink in _sinks)
            {
                sink.Flush();
            }
        }

        public IEnumerator<ITelemetryWriter> GetEnumerator() => _sinks.GetEnumerator();

        public void Write(TelemetryEvent telemetryEvent)
        {
            using (telemetryEvent.KeepAlive())
            {
                foreach (var sink in _sinks)
                {
                    sink.Write(telemetryEvent);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => _sinks.GetEnumerator();
    }
}
