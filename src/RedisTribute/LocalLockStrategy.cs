using RedisTribute.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class LocalLockStrategy : IAsyncLockStrategy<IAsyncLock>
    {
        readonly AsyncLock _lock;

        public LocalLockStrategy(TimeSpan? timeout = null)
        {
            _lock = new AsyncLock(timeout.GetValueOrDefault(TimeSpan.FromSeconds(5)));
        }

        public async Task<IAsyncLock> AquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default)
        {
            var localLock = await _lock.LockAsync(cancellation);

            return new LockHandle(localLock);
        }

        private class LockHandle : IAsyncLock
        {
            readonly IDisposable _lockHandle;

            public LockHandle(IDisposable lockHandle)
            {
                _lockHandle = lockHandle;
            }

            public void Dispose() => _lockHandle.Dispose();

            public Task ReleaseLockAsync(CancellationToken cancellation = default)
            {
                _lockHandle.Dispose();
                return Task.CompletedTask;
            }
        }
    }
}
