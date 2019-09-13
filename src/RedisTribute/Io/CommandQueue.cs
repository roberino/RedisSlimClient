using RedisTribute.Io.Commands;
using RedisTribute.Io.Scheduling;
using RedisTribute.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class CommandQueue : IDisposable
    {
        readonly AsyncLock _lock;
        readonly Queue<IRedisCommand> _commandQueue;

        public CommandQueue()
        {
            _commandQueue = new Queue<IRedisCommand>();
            _lock = new AsyncLock();
        }

        public int QueueSize => _commandQueue.Count;

        public async Task AbortAll(Exception ex, IWorkScheduler scheduler)
        {
            IRedisCommand[] cmds = null;

            await AccessQueue(q =>
            {
                cmds = q.ToArray();

                q.Clear();

                return true;
            });

            foreach (var cmd in cmds)
            {
                scheduler.Schedule(() =>
                {
                    cmd.Abandon(ex);
                    return Task.CompletedTask;
                });
            }
        }

        public async Task Requeue(Func<Task> synchronisedWork)
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

                _commandQueue.Enqueue(command);
            }
        }

        public bool ProcessNextCommand(Action<IRedisCommand> action)
        {
            return AccessQueue(x => ProcessNextCommandInternal(action)).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        bool ProcessNextCommandInternal(Action<IRedisCommand> action)
        {
            if (_commandQueue.Count > 0)
            {
                action(_commandQueue.Dequeue());

                return true;
            }

            return false;
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

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}