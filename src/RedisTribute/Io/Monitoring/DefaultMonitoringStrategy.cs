using RedisTribute.Telemetry;
using RedisTribute.Types.Primatives;
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
                var start = TelemetryEvent.CreateStart(nameof(OnHeartbeat));

                ThreadPool.GetAvailableThreads(out var wt, out var cpt);

                foreach (var result in client.PingAllAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    if (_telemetryWriter.Enabled && _telemetryWriter.Category.HasFlag(TelemetryCategory.Health))
                    {
                        var endEv = start.CreateChild(nameof(OnHeartbeat));

                        endEv.Dimensions["WT"] = wt;
                        endEv.Dimensions["CPT"] = cpt;
                        endEv.Dimensions["Role"] = result.Endpoint.Scheme;
                        endEv.Dimensions[nameof(StreamPool.PooledMemory)] = StreamPool.Instance.PooledMemory;
                        endEv.Dimensions[nameof(Uri.Host)] = result.Endpoint.Host;
                        endEv.Dimensions[nameof(Uri.Port)] = result.Endpoint.Port;
                        endEv.Category = TelemetryCategory.Health;
                        endEv.Exception = result.Error;
                        endEv.Severity = result.Ok ? Severity.Error : Severity.Info;
                        endEv.Elapsed = result.Elapsed;

                        _telemetryWriter.Write(endEv);
                    }
                }
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