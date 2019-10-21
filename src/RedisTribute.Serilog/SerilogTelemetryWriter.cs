using RedisTribute.Telemetry;
using Serilog;

namespace RedisTribute.Serilog
{
    class SerilogTelemetryWriter : ITelemetryWriter
    {
        readonly ILogger _logger;

        public SerilogTelemetryWriter(ILogger logger)
        {
            _logger = logger;
        }
        public TelemetryCategory Category => TelemetryCategory.Health | TelemetryCategory.Internal | TelemetryCategory.Request;
        public Severity Severity => Severity.Error | Severity.Info;
        public bool Enabled => true;

        public void Write(TelemetryEvent ev)
        {
            if (ev.Category == TelemetryCategory.Request && ev.Sequence == TelemetrySequence.End)
            {
                _logger.Information("Request: {@ev}", ev);

                return;
            }

            if (ev.Category == TelemetryCategory.Health)
            {
                _logger.Information("Health: {@ev}", ev);

                return;
            }

            if (ev.Category == TelemetryCategory.Internal && ev.Exception != null)
            {
                _logger.Error(ev.Exception, "Internal Error");
            }
        }
    }
}