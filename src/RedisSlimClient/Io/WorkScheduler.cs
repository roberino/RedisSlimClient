using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class WorkScheduler : IWorkScheduler
    {
        bool _disposed;

        public void Awake() { }

        public void Dispose()
        {
            _disposed = true;
        }

        public void Schedule(Func<bool> work)
        {
            Task.Run(() =>
            {
                while (!_disposed)
                {
                    work();
                }
            });
        }
    }
}