using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    public interface IAsyncLock : IDisposable
    {
        Task ReleaseLockAsync(CancellationToken cancellation = default);
    }

    public interface IDistributedLock : IAsyncLock
    {
        string Key { get; }
        DateTime Created { get; }
        TimeSpan RemainingTime { get; }
        bool LockExpired { get; }
    }

    public readonly struct LockOptions
    {
        public LockOptions(TimeSpan maxLockTime, bool allowRecursions)
        {
            MaxLockTime = maxLockTime;
            AllowRecursion = allowRecursions;
        }

        private LockOptions(bool allowRecursions)
        {
            MaxLockTime = null;
            AllowRecursion = allowRecursions;
        }

        public static LockOptions AllowRecursiveLocks => new LockOptions(true);

        public TimeSpan? MaxLockTime { get; }

        public bool AllowRecursion { get; }
    }

    public readonly struct NullLock : IAsyncLockStrategy<IAsyncLock>, IAsyncLockStrategy<IDistributedLock>
    {
        public Task<IDistributedLock> AcquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default)
        {
            return Task.FromResult((IDistributedLock)new LockImpl() { Key = key });
        }

        Task<IAsyncLock> IAsyncLockStrategy<IAsyncLock>.AcquireLockAsync(string key, LockOptions options, CancellationToken cancellation)
        {
            return Task.FromResult((IAsyncLock)new LockImpl() { Key = key });
        }

        private class LockImpl : IDistributedLock
        {
            public string Key { get; set; } = string.Empty;
            public DateTime Created { get; } = DateTime.UtcNow;
            public TimeSpan RemainingTime => TimeSpan.MaxValue;
            public bool LockExpired => false;

            public void Dispose()
            {
            }

            public Task ReleaseLockAsync(CancellationToken cancellation = default)
            {
                return Task.CompletedTask;
            }
        }
    }

    public interface IAsyncLockStrategy<T> where T : IAsyncLock
    {
        Task<T> AcquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default);
    }

    /// <summary>
    /// See https://redis.io/topics/distlock
    /// </summary>
    public interface IRedLock : IAsyncLockStrategy<IDistributedLock>
    {
    }
}
