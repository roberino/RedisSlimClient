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
                var baseName = component.GetType().Name;
                var opId = TelemetryEvent.CreateId();
                var sw = new Stopwatch();

                sw.Start();

                component.StateChanged += s =>
                {
                    var childEvent = new TelemetryEvent()
                    {
                        Name = $"{baseName}/{s}",                        
                        Elapsed = sw.Elapsed,
                        OperationId = opId,
                        Data = component.EndpointIdentifier.ToString(),
                        Severity = s == PipelineStatus.Faulted ? Severity.Error : Severity.Diagnostic
                    };

                    childEvent.AddComponentInf(component);

                    writer.Write(childEvent);
                };

                component.Error += e =>
                {
                    var childEvent = new TelemetryEvent()
                    {
                        Name = nameof(component.Error),
                        Elapsed = sw.Elapsed,
                        OperationId = opId,
                        Data = component.EndpointIdentifier.ToString(),
                        Severity = Severity.Error,
                        Exception = e
                    };

                    childEvent.AddComponentInf(component);

                    writer.Write(childEvent);
                };
            }
        }

        static void AddComponentInf(this TelemetryEvent childEvent, IPipelineComponent component)
        {
            childEvent.Dimensions[$"{nameof(Uri.Host)}"] = component.EndpointIdentifier.Host;
            childEvent.Dimensions[$"{nameof(Uri.Port)}"] = component.EndpointIdentifier.Port;
            childEvent.Dimensions["Role"] = component.EndpointIdentifier.Scheme;
        }
    }
}