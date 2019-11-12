using RedisTribute.Telemetry;
using System;
using System.Diagnostics;
using System.Text;

namespace RedisTribute.Io.Net
{
    static class TelemetryExtensions
    {
        public static void AttachTelemetry(this ISocket socket, ITelemetryWriter writer)
        {
            if (writer.Enabled)
            {
                var opId = TelemetryEvent.CreateId();
                var sw = new Stopwatch();
                var baseName = socket.GetType().Name;

                sw.Start();

                if (writer.Severity.HasFlag(Severity.Diagnostic) && socket is ITraceable traceable)
                {
                    traceable.Trace += e =>
                    {
                        var childEvent = TelemetryEventFactory.Instance.Create($"{baseName}/{e.Action}", opId);

                        childEvent.Elapsed = sw.Elapsed;
                        childEvent.Data = $"{socket.EndpointIdentifier} ({e.Data.Length} bytes): {Encoding.ASCII.GetString(e.Data)}";
                        childEvent.Severity = Severity.Diagnostic;

                        childEvent.Dimensions[$"{nameof(Uri.Host)}"] = socket.EndpointIdentifier.Host;
                        childEvent.Dimensions[$"{nameof(Uri.Port)}"] = socket.EndpointIdentifier.Port;
                        childEvent.Dimensions["Role"] = socket.EndpointIdentifier.Scheme;

                        writer.Write(childEvent);
                    };
                }

                socket.Receiving += s =>
                {
                    var level = s == ReceiveStatus.Faulted ? Severity.Error : Severity.Diagnostic;

                    if (writer.Severity.HasFlag(level))
                    {
                        var childEvent = TelemetryEventFactory.Instance.Create($"{baseName}/{s}", opId);

                        childEvent.Data = socket.EndpointIdentifier.ToString();
                        childEvent.Severity = level;
                        childEvent.Elapsed = sw.Elapsed;

                        childEvent.Dimensions[$"{nameof(Uri.Host)}"] = socket.EndpointIdentifier.Host;
                        childEvent.Dimensions[$"{nameof(Uri.Port)}"] = socket.EndpointIdentifier.Port;
                        childEvent.Dimensions["Role"] = socket.EndpointIdentifier.Scheme;

                        writer.Write(childEvent);
                    }
                };
            }
        }
    }
}