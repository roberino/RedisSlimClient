using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Scheduling
{
    class WorkScheduler : IWorkScheduler
    {
        readonly object _lockObj = new object();
        bool _disposed;

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
                    if (!work())
                    {
                        lock (_lockObj)
                        {
                            if (!work())
                            {
                                Monitor.Wait(_lockObj, 10);
                            }
                        }
                    }
                }
            });
        }
    }
}