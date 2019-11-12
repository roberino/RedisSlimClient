using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using RedisTribute.ApplicationInsights;
using RedisTribute.Configuration;

namespace RedisTribute
{
    public static class ApplicationInsightsExtensions
    {
        public static ClientConfiguration UseApplicationInsights(this ClientConfiguration clientConfiguration, TelemetryConfiguration telemetryConfiguration = null)
        {
            return clientConfiguration.UseApplicationInsights(new TelemetryClient(telemetryConfiguration));
        }

        public static ClientConfiguration UseApplicationInsights(this ClientConfiguration clientConfiguration, string instrumentationKey)
        {
            return clientConfiguration.UseApplicationInsights(new TelemetryConfiguration(instrumentationKey));
        }

        public static ClientConfiguration UseApplicationInsights(this ClientConfiguration clientConfiguration, TelemetryClient telemetryClient)
        {
            clientConfiguration.TelemetrySinks.Add(new ApplicationInsightsTelemetryWriter(telemetryClient));

            return clientConfiguration;
        }
    }
}
