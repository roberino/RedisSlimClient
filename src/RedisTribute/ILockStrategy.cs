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

    public interface IAsyncLockStrategy<T> where T : IAsyncLock
    {
        Task<T> AquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default);
    }

    /// <summary>
    /// See https://redis.io/topics/distlock
    /// </summary>
    public interface IRedLock : IAsyncLockStrategy<IDistributedLock>
    {
    }
}
