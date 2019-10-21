using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using RedisTribute.Telemetry;
using System;

namespace RedisTribute.ApplicationInsights
{
    class ApplicationInsightsTelemetryWriter : ITelemetryWriter
    {
        readonly TelemetryClient _telemetryClient;

        DateTime? _traceTtl;

        public ApplicationInsightsTelemetryWriter(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public TimeSpan? ProactiveTrace { get; set; }
        public TelemetryCategory Category => TelemetryCategory.Health | TelemetryCategory.Internal | TelemetryCategory.Request;
        public Severity Severity => Severity.Error | Severity.Info;
        public bool Enabled => true;

        public void Write(TelemetryEvent ev)
        {
            var now = DateTime.UtcNow;

            if (ev.Severity == Severity.Error && ProactiveTrace.HasValue)
            {
                _traceTtl = now.Add(ProactiveTrace.Value);
            }

            if (ev.Severity == Severity.Diagnostic && ev.Category == TelemetryCategory.Internal && _traceTtl.HasValue && _traceTtl.Value > now)
            {
                var trace = CopyDimentions(ev, new TraceTelemetry($"{ev.Name}/{ev.Data}", SeverityLevel.Warning));

                _telemetryClient.TrackTrace(trace);

                return;
            }

            ev.Dimensions.TryGetValue(nameof(Uri.Host), out var host);
            ev.Dimensions.TryGetValue(nameof(Uri.Port), out var port);

            var target = host == null ? "unknown" : $"{host}:{port}";

            if (ev.Category == TelemetryCategory.Request && ev.Sequence == TelemetrySequence.End)
            {
                var telemetryItem = CopyDimentions(ev, new DependencyTelemetry("REDIS", target, ev.Name, ev.Data)
                {
                    Timestamp = now.AddMilliseconds(ev.Elapsed.TotalMilliseconds),
                    Duration = ev.Elapsed
                });

                _telemetryClient.TrackDependency(telemetryItem);

                return;
            }

            if (ev.Category == TelemetryCategory.Health)
            {
                var telemetryItem = CopyDimentions(ev, new AvailabilityTelemetry("RedisTribute", now, ev.Elapsed, target, ev.Exception == null, ev.Data));

                // telemetryItem.Properties["ProcessMemory"] = Process.GetCurrentProcess().WorkingSet64.ToString();

                _telemetryClient.TrackAvailability(telemetryItem);
            }

            if (ev.Category == TelemetryCategory.Internal && ev.Exception != null)
            {
                _telemetryClient.TrackException(ev.Exception);
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