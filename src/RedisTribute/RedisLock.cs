using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Commands.Scripts;
using RedisTribute.Io.Scheduling;
using RedisTribute.Types;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisLock : IRedLock
    {
        readonly RedisController _controller;
        readonly ConcurrentDictionary<string, LockRelease> _currentLocks;
        readonly TimeSpan _defaultLockTime;

        public RedisLock(RedisController commandRouter)
        {
            _controller = commandRouter;
            _currentLocks = new ConcurrentDictionary<string, LockRelease>();
            _defaultLockTime = TimeSpan.FromSeconds(5);
        }

        public async Task<IDistributedLock> AquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default)
        {
            var now = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();
            var ctx = Guid.NewGuid().ToString("N");
            var data = Encoding.ASCII.GetBytes(ctx);
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var localKey = $"{key}${threadId}";

            var heldTime = options.MaxLockTime.GetValueOrDefault(_defaultLockTime);

            if (options.AllowRecursion)
            {
                if (_currentLocks.TryGetValue(localKey, out var eLock))
                {
                    if (eLock.LockExpired || (options.MaxLockTime.HasValue && options.MaxLockTime.Value > eLock.RemainingTime))
                    {
                        throw LockNotObtained("Expired");
                    }

                    var subLock = new LockRelease(key, now, eLock.MaxLockTime, (k, c) =>
                    {
                        _currentLocks[localKey] = eLock;

                        return Task.CompletedTask;
                    }, false);

                    _currentLocks[localKey] = subLock;

                    return subLock;
                }
            }

            while (!cancellation.IsCancellationRequested)
            {
                var opts = new SetOptions(heldTime, SetCondition.SetKeyIfNotExists);
                var cmd = new Func<IRedisResult<bool>>(() => new SetCommand(key, data, opts));

                var results = await _controller.GetResponses(cmd, (x, c, m) => (ok: c, e: null as Exception), (r, e, m) => (ok: false, e), ConnectionTarget.AllAvailableMasters, cancellation);

                if (results.Length == 0)
                {
                    throw new NoAvailableConnectionException();
                }

                var pc = results.Count(x => x.ok) / (float)results.Length;

                if (pc < 0.66f)
                {
                    if (cancellation != default)
                    {
                        continue;
                    }

                    throw LockNotObtained(results.Where(r => !r.ok).First().e?.Message ?? "Key Locked");
                }

                var lockHandle = new LockRelease(key, now, heldTime, (k, ct) => ReleaseLockAsync(k, data, localKey, ct));

                _currentLocks[localKey] = lockHandle;

                return lockHandle;
            }

            throw LockNotObtained("Cancelled");
        }

        private async Task ReleaseLockAsync(string key, byte[] data, string localKey, CancellationToken cancellation)
        {
            try
            {
                var lua = ScriptFactory.ReleaseLock;

                var cmd = new Func<IRedisResult<IRedisObject>>(() => new EvalCommand(lua, new RedisKey[] { key }, data));

                var results = await _controller.GetResponses(cmd, (x, c, m) => (true, null as Exception), (x, e, m) => (false, e), ConnectionTarget.AllAvailableMasters, cancellation);
            }
            finally
            {
                _currentLocks.TryRemove(localKey, out _);
            }
        }

        static Exception LockNotObtained(string reason) => new SynchronizationLockException($"Lock not obtained: {reason}");

        class LockRelease : IDistributedLock
        {
            readonly bool _allowAsyncDispose;
            readonly Func<string, CancellationToken, Task> _onRelease;
            long _released = 0;

            public LockRelease(string key, DateTime created, TimeSpan maxLockTime, Func<string, CancellationToken, Task> onRelease, bool allowAsyncDispose = true)
            {
                Key = key;
                Created = created;
                MaxLockTime = maxLockTime;
                _onRelease = onRelease;
                _allowAsyncDispose = allowAsyncDispose;
            }

            public string Key { get; }
            public DateTime Created { get; }
            public TimeSpan MaxLockTime { get; }
            public TimeSpan RemainingTime => MaxLockTime - (DateTime.UtcNow - Created);
            public bool LockExpired => RemainingTime.Milliseconds < 0 || Interlocked.Read(ref _released) == 1;

            public void Dispose()
            {
                if (_allowAsyncDispose)
                {
                    if (Interlocked.Read(ref _released) == 0)
                    {
                        ThreadPoolScheduler.Instance.Schedule(() => ReleaseLockAsync());
                    }
                    return;
                }

                ReleaseLockAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            public async Task ReleaseLockAsync(CancellationToken cancellation = default)
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    try
                    {
                        await _onRelease(Key, cancellation);
                    }
                    catch
                    {
                        Interlocked.Exchange(ref _released, 0);
                        throw;
                    }
                }
            }
        }
    }
}