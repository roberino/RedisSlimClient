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
                var start = TelemetryEventFactory.Instance.CreateStart(nameof(OnHeartbeat));

                var threads = EnvironmentData.GetThreadPoolUsage();

                foreach (var result in client.PingAllAsync().ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    if (_telemetryWriter.Enabled && _telemetryWriter.Category.HasFlag(TelemetryCategory.Health))
                    {
                        var endEv = TelemetryEventFactory.Instance.Create(start.Name, start.OperationId);

                        endEv.Dimensions["WT"] = threads.WorkerThreads;
                        endEv.Dimensions["CPT"] = threads.IoThreads;
                        endEv.Dimensions["MinWT"] = threads.MinWorkerThreads;
                        endEv.Dimensions["MinCPT"] = threads.MinIoThreads;
                        endEv.Dimensions["Role"] = result.Endpoint.Scheme;
                        endEv.Dimensions[nameof(client.ClientName)] = client.ClientName;
                        endEv.Dimensions[nameof(result.Metrics.PendingCommands)] = result.Metrics.PendingCommands;
                        endEv.Dimensions[nameof(result.Metrics.PendingReads)] = result.Metrics.PendingReads;
                        endEv.Dimensions[nameof(result.Metrics.Workload)] = result.Metrics.Workload;
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