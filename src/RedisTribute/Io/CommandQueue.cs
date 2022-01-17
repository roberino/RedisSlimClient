using RedisTribute.Io.Commands;
using RedisTribute.Io.Scheduling;
using RedisTribute.Types;
using RedisTribute.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class CommandQueue : ICommandQueue
    {
        readonly AsyncLock _lock;
        readonly Queue<IRedisCommand> _commandQueue;

        public CommandQueue()
        {
            _commandQueue = new Queue<IRedisCommand>();
            _lock = new AsyncLock();
        }

        public virtual int QueueSize => _commandQueue.Count;

        public virtual async Task AbortAll(Exception ex, IWorkScheduler scheduler)
        {
            IRedisCommand[]? cmds = null;

            await AccessQueue(q =>
            {
                cmds = q.ToArray();

                q.Clear();

                return true;
            });

            foreach (var cmd in cmds!)
            {
                scheduler.Schedule(() =>
                {
                    cmd.Abandon(ex);
                    return Task.CompletedTask;
                });
            }
        }

        public virtual async Task Requeue(Func<Task> synchronisedWork)
        {
            IRedisCommand[] salvagable;

            using (await _lock.LockAsync())
            {
                salvagable = _commandQueue.ToArray();

                Clear();
            }

            await synchronisedWork();

            if (salvagable.Length == 0)
            {
                return;
            }

            using (await _lock.LockAsync())
            {
                foreach (var command in salvagable)
                {
                    if (!command.CanBeCompleted)
                    {
                        continue;
                    }

                    await command.Execute();

                    _commandQueue.Enqueue(command);
                }
            }
        }

        public async Task Enqueue(IRedisCommand command, CancellationToken cancellation = default)
        {
            using (await _lock.LockAsync(cancellation))
            {
                await command.Execute();

                Attach(command);
            }
        }

        public Func<Task> BindResult(IRedisObject result)
        {
            return AccessQueue(_ => Bind(result)).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        protected virtual void Attach(IRedisCommand command)
        {
            _commandQueue.Enqueue(command);
        }

        protected virtual Func<Task> Bind(IRedisObject result)
        {
            if (_commandQueue.Count > 0)
            {
                var next = _commandQueue.Dequeue();

                return () => Task.FromResult(next.SetResult(result));
            }

            return () => Task.CompletedTask;
        }

        async Task<T> AccessQueue<T>(Func<Queue<IRedisCommand>, T> work, CancellationToken cancellation = default)
        {
            using (await _lock.LockAsync(cancellation))
            {
                return work(_commandQueue);
            }
        }

        void Clear()
        {
            _commandQueue.Clear();
        }

        public virtual void Dispose()
        {
            _lock.Dispose();
        }
    }
}