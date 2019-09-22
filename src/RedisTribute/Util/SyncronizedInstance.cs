using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Util
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

        public TValue TryGet<TValue>(Func<T, TValue> valueFactory, TValue defaultValue = default)
        {
            var instance = _instance;

            if (instance != null)
            {
                return valueFactory(instance);
            }

            return defaultValue;
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
            if (_instance != null)
            {
                return _instance;
            }

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
                CleanupInstance();

                _instance = default;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            CleanupInstance();

            _semaphore.Dispose();
        }

        void CleanupInstance()
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
        }
    }
}