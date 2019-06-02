using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    class AsyncLock<T> : IDisposable
    {
        static readonly TimeSpan _defaultTimeout = TimeSpan.FromMilliseconds(30);

        readonly SemaphoreSlim _semaphore;
        readonly Func<Task<T>> _factory;

        T _instance;

        public AsyncLock(Func<Task<T>> factory)
        {
            _factory = factory;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public TValue TryGet<TValue>(Func<T, TValue> valueFactory)
        {
            var instance = _instance;

            if (instance != null)
            {
                return valueFactory(instance);
            }

            return default;
        }

        public async Task Execute(Func<T, Task> work, TimeSpan? timeout = null)
        {
            await _semaphore.WaitAsync(timeout.GetValueOrDefault(_defaultTimeout));

            try
            {
                if (_instance == null)
                {
                    _instance = await _factory();
                }

                await work(_instance);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> GetValue(TimeSpan? timeout = null)
        {
            await _semaphore.WaitAsync(timeout.GetValueOrDefault(_defaultTimeout));

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
