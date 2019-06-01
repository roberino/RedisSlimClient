using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    class AsyncLock<T> : IDisposable
    {
        readonly SemaphoreSlim _semaphore;
        readonly Func<Task<T>> _factory;

        T _instance;

        public AsyncLock(Func<Task<T>> factory)
        {
            _factory = factory;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task Execute(Func<T, Task> work)
        {
            await _semaphore.WaitAsync();

            try
            {
                var instance = await GetValue();

                await work(instance);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> GetValue()
        {
            await _semaphore.WaitAsync();

            try
            {
                if (_instance == null)
                {
                    _instance = await _factory();
                }

                return _instance;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_instance is IDisposable d)
            {
                d.Dispose();
            }

            _semaphore.Dispose();
        }
    }
}
