using RedisTribute.Telemetry;
using System;
using System.Threading;

namespace RedisTribute.Io.Monitoring
{
    class DefaultMonitoringStrategy : IDisposable
    {
        TimeSpan DefaultInterval => TimeSpan.FromSeconds(1);

        readonly Timer _timer;
        readonly ITelemetryWriter _telemetryWriter;

        public DefaultMonitoringStrategy(IRedisDiagnosticClient client, ITelemetryWriter telemetryWriter, TimeSpan? heartbeatInterval)
        {
            _timer = new Timer(x => OnHeartbeat((IRedisDiagnosticClient)x), client, heartbeatInterval.GetValueOrDefault(DefaultInterval), heartbeatInterval.GetValueOrDefault(DefaultInterval));
            _telemetryWriter = telemetryWriter;
        }

        void OnHeartbeat(IRedisDiagnosticClient client)
        {
            try
            {
                _telemetryWriter.ExecuteAsync(c =>
                {
                    ThreadPool.GetAvailableThreads(out var wt, out var cpt);

                    c.Dimensions["WT"] = wt;
                    c.Dimensions["CPT"] = cpt;

                    return client.PingAllAsync();

                }, nameof(OnHeartbeat), Severity.Diagnostic).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}