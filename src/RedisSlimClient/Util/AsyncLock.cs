using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    class SyncronizedInstance<T> : IDisposable
    {
        readonly TimeSpan _timeout;
        readonly SemaphoreSlim _semaphore;
        readonly Func<Task<T>> _factory;

        T _instance;

        public SyncronizedInstance(Func<Task<T>> factory, TimeSpan? timeout = null)
        {
            _factory = factory;
            _semaphore = new SemaphoreSlim(1, 1);
            _timeout = timeout.GetValueOrDefault(TimeSpan.FromSeconds(30));
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
            await _semaphore.WaitAsync(_timeout);

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

        public async Task<T> GetValue()
        {
            await _semaphore.WaitAsync(_timeout);

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

        public async Task Reset()
        {
            await _semaphore.WaitAsync(_timeout);

            try
            {
                (_instance as IDisposable)?.Dispose();

                _instance = default;
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

            if (_instance is IEnumerable e)
            {
                foreach (var x in e)
                {
                    if (x is IDisposable ed)
                    {
                        ed.Dispose();
                    }
                }
            }

            _semaphore.Dispose();
        }
    }
}