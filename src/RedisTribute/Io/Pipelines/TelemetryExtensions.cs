using RedisTribute.Telemetry;
using System;
using System.Diagnostics;
using System.Text;

namespace RedisTribute.Io.Pipelines
{
    static class TelemetryExtensions
    {
        public static void AttachTelemetry(this IDuplexPipeline component, ITelemetryWriter writer)
        {
            if (!writer.Enabled)
            {
                return;
            }

            var opId = TelemetryEvent.CreateId();
            var sw = new Stopwatch();

            sw.Start();

            component.Receiver.AttachTelemetry(writer, opId, sw);
            component.Sender.AttachTelemetry(writer, opId, sw);
        }

        static void AttachTelemetry(this IPipelineComponent component, ITelemetryWriter writer, string opId, Stopwatch sw)
        {
            var baseName = component.GetType().Name;

            if (writer.Severity.HasFlag(Severity.Diagnostic))
            {
                component.Trace += e =>
                {
                    var childEvent = new TelemetryEvent()
                    {
                        Name = $"{baseName}/{e.Action}",
                        Elapsed = sw.Elapsed,
                        OperationId = opId,
                        Data = $"{component.EndpointIdentifier} ({e.Data.Length} bytes): {Encoding.ASCII.GetString(e.Data)}",
                        Severity = Severity.Diagnostic
                    };

                    childEvent.Dimensions[$"{nameof(Uri.Host)}"] = component.EndpointIdentifier.Host;
                    childEvent.Dimensions[$"{nameof(Uri.Port)}"] = component.EndpointIdentifier.Port;
                    childEvent.Dimensions["Role"] = component.EndpointIdentifier.Scheme;

                    writer.Write(childEvent);
                };
            }

            component.StateChanged += s =>
            {
                var status = s == PipelineStatus.Faulted ? Severity.Error : Severity.Diagnostic;

                if (writer.Severity.HasFlag(status))
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
                }
            };

            component.Error += e =>
            {
                if (writer.Severity.HasFlag(Severity.Error))
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
                }
            };
        }

        static void AddComponentInf(this TelemetryEvent childEvent, IPipelineComponent component)
        {
            childEvent.Dimensions[$"{nameof(Uri.Host)}"] = component.EndpointIdentifier.Host;
            childEvent.Dimensions[$"{nameof(Uri.Port)}"] = component.EndpointIdentifier.Port;
            childEvent.Dimensions["Role"] = component.EndpointIdentifier.Scheme;
        }
    }
}