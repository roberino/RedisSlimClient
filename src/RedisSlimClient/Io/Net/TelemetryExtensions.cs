using RedisSlimClient.Telemetry;
using System;
using System.Diagnostics;

namespace RedisSlimClient.Io.Net
{
    static class TelemetryExtensions
    {
        public static void AttachTelemetry(this ISocket component, ITelemetryWriter writer)
        {
            if (writer.Enabled)
            {
                var opId = TelemetryEvent.CreateId();
                var sw = new Stopwatch();

                sw.Start();

                component.Receiving += s =>
                {
                    var childEvent = new TelemetryEvent()
                    {
                        Name = s.ToString(),                        
                        Elapsed = sw.Elapsed,
                        OperationId = opId,
                        Data = component.EndpointIdentifier.ToString(),
                        Severity = s == ReceiveStatus.Faulted ? Severity.Error : Severity.Diagnostic
                    };

                    childEvent.Dimensions[$"{nameof(Uri.Host)}"] = component.EndpointIdentifier.Host;
                    childEvent.Dimensions[$"{nameof(Uri.Port)}"] = component.EndpointIdentifier.Port;
                    childEvent.Dimensions["Role"] = component.EndpointIdentifier.Scheme;

                    writer.Write(childEvent);
                };
            }
        }
    }
}