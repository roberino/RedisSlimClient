using RedisTribute.Configuration;
using RedisTribute.Serilog;
using Serilog;

namespace RedisTribute
{
    public static class SerilogExtensions
    {
        public static ClientConfiguration UseSerilog(this ClientConfiguration clientConfiguration, LoggerConfiguration loggerConfiguration)
        {
            clientConfiguration.TelemetrySinks.Add(new SerilogTelemetryWriter(loggerConfiguration.CreateLogger()));

            return clientConfiguration;
        }
    }
}