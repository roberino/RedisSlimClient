using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Util
{
    class AsyncLock : IDisposable
    {
        readonly TimeSpan _timeout;
        readonly SemaphoreSlim _lock;

        public AsyncLock(TimeSpan? timeout = null)
        {
            _timeout = timeout.GetValueOrDefault(TimeSpan.FromMilliseconds(10000));
            _lock = new SemaphoreSlim(1, 1);
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public async Task AwaitAsync()
        {
            using (await LockAsync()) { }
        }

        public Task<IDisposable> LockAsync()
        {
            return Locker.GetAsync(_lock, _timeout);
        }

        public Task<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            return Locker.GetAsync(_lock, cancellationToken);
        }

        class Locker : IDisposable
        {
            readonly SemaphoreSlim _semaphore;

            Locker(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public static async Task<IDisposable> GetAsync(SemaphoreSlim semaphore, TimeSpan timeout)
            {
                await semaphore.WaitAsync(timeout);

                return new Locker(semaphore);
            }

            public static async Task<IDisposable> GetAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
            {
                await semaphore.WaitAsync(cancellationToken);

                return new Locker(semaphore);
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
