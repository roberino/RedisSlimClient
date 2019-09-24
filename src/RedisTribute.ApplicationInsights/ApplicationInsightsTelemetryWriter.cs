using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using RedisTribute.Telemetry;
using System;

namespace RedisTribute.ApplicationInsights
{
    class ApplicationInsightsTelemetryWriter : ITelemetryWriter
    {
        readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTelemetryWriter(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public TelemetryCategory Category { get; } = TelemetryCategory.Health | TelemetryCategory.Internal | TelemetryCategory.Request;
        public Severity Severity { get; } = Severity.Error | Severity.Info;
        public bool Enabled { get; } = true;

        public void Write(TelemetryEvent telemetryEvent)
        {
            var now = DateTime.UtcNow;

            telemetryEvent.Dimensions.TryGetValue(nameof(Uri.Host), out var host);
            telemetryEvent.Dimensions.TryGetValue(nameof(Uri.Port), out var port);

            var target = host == null ? "unknown" : $"{host}:{port}";

            if (telemetryEvent.Category == TelemetryCategory.Request && telemetryEvent.Sequence == TelemetrySequence.End)
            {
                var telemetryItem = CopyDimentions(telemetryEvent, new DependencyTelemetry("REDIS", target, telemetryEvent.Name, telemetryEvent.Data)
                {
                    Timestamp = now.AddMilliseconds(telemetryEvent.Elapsed.TotalMilliseconds),
                    Duration = telemetryEvent.Elapsed
                });

                _telemetryClient.TrackDependency(telemetryItem);

                return;
            }

            if (telemetryEvent.Category == TelemetryCategory.Health)
            {
                var telemetryItem = CopyDimentions(telemetryEvent, new AvailabilityTelemetry("RedisTribute", now, telemetryEvent.Elapsed, target, telemetryEvent.Exception == null, telemetryEvent.Data));

                _telemetryClient.TrackAvailability(telemetryItem);
            }

            if (telemetryEvent.Category == TelemetryCategory.Internal && telemetryEvent.Exception != null)
            {
                _telemetryClient.TrackException(telemetryEvent.Exception);
            }
        }

        static T CopyDimentions<T>(TelemetryEvent ev, T item)
            where T : ISupportProperties
        {
            foreach (var dim in ev.Dimensions)
            {
                item.Properties[dim.Key] = dim.Value?.ToString();
            }

            return item;
        }
    }
}