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

        public static ClientConfiguration UseApplicationInsights(this ClientConfiguration clientConfiguration, TelemetryClient telemetryClient)
        {
            clientConfiguration.TelemetryWriter = new ApplicationInsightsTelemetryWriter(telemetryClient);

            return clientConfiguration;
        }
    }
}
