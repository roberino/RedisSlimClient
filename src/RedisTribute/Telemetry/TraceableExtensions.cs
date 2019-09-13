using System.Diagnostics;
using System.Text;

namespace RedisTribute.Telemetry
{
    static class TraceableExtensions
    {
        public static T AttachTelemetry<T>(this T traceable, ITelemetryWriter writer, Severity severity = Severity.Diagnostic) where T : ITraceable
        {
            if (writer == null || !writer.Enabled || !writer.Severity.HasFlag(severity))
            {
                return traceable;
            }

            var opId = TelemetryEvent.CreateId();
            var baseName = traceable.GetType().Name;

            var sw = new Stopwatch();

            sw.Start();

            traceable.Trace += e =>
            {
                var childEvent = new TelemetryEvent()
                {
                    Name = $"{baseName}/{e.Action}",
                    Elapsed = sw.Elapsed,
                    OperationId = opId,
                    Data = $"({e.Data.Length} bytes): {Encoding.ASCII.GetString(e.Data)}",
                    Severity = Severity.Diagnostic
                };

                writer.Write(childEvent);
            };

            return traceable;
        }
    }
}