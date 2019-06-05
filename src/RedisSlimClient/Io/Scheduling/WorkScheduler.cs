using RedisSlimClient.Telemetry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    class WorkScheduler : IWorkScheduler
    {
        readonly object _lockObj = new object();
        readonly ITelemetryWriter _telemetryWriter;
        bool _disposed;

        public WorkScheduler(ITelemetryWriter telemetryWriter)
        {
            _telemetryWriter = telemetryWriter;
        }

        public void Awake()
        {
            lock (_lockObj)
            {
                Monitor.Pulse(_lockObj);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                lock (_lockObj)
                {
                    Monitor.PulseAll(_lockObj);
                }
            }
        }

        public void Schedule(Func<bool> work)
        {
            Task.Run(() =>
            {
                while (!_disposed)
                {
                    try
                    {
                        _telemetryWriter.ExecuteAsync(ctx =>
                        {
                            if (!work())
                            {
                                lock (_lockObj)
                                {
                                    if (!_disposed && !work())
                                    {
                                        Monitor.Wait(_lockObj, 100);
                                    }
                                }
                            }
                            return Task.FromResult(true);
                        }, nameof(work))
                        .ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                }
            });
        }
    }
}