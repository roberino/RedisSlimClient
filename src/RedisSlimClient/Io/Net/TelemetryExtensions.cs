using RedisSlimClient.Telemetry;
using System;
using System.Diagnostics;
using System.Text;

namespace RedisSlimClient.Io.Net
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
                        var childEvent = new TelemetryEvent()
                        {
                            Name = $"{baseName}/{e.Action}",
                            Elapsed = sw.Elapsed,
                            OperationId = opId,
                            Data = $"{socket.EndpointIdentifier}: {Encoding.ASCII.GetString(e.Data)}",
                            Severity = Severity.Diagnostic
                        };

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
                        var childEvent = new TelemetryEvent()
                        {
                            Name = $"{baseName}/{s}",
                            Elapsed = sw.Elapsed,
                            OperationId = opId,
                            Data = socket.EndpointIdentifier.ToString(),
                            Severity = level
                        };

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