using RedisSlimClient.Telemetry;
using System;
using System.Diagnostics;

namespace RedisSlimClient.Io.Pipelines
{
    static class TelemetryExtensions
    {
        public static void AttachTelemetry(this IDuplexPipeline component, ITelemetryWriter writer)
        {
            component.Receiver.AttachTelemetry(writer);
            component.Sender.AttachTelemetry(writer);
        }

        public static void AttachTelemetry(this IPipelineComponent component, ITelemetryWriter writer)
        {
            if (writer.Enabled)
            {
                var opId = TelemetryEvent.CreateId();
                var sw = new Stopwatch();

                sw.Start();

                component.StateChanged += s =>
                {
                    var childEvent = new TelemetryEvent()
                    {
                        Name = s.ToString(),                        
                        Elapsed = sw.Elapsed,
                        OperationId = opId,
                        Data = component.EndpointIdentifier.ToString(),
                        Severity = s == PipelineStatus.Faulted ? Severity.Error : Severity.Info
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