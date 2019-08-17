using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Scheduling;

namespace RedisSlimClient.Io
{
    class CommandQueue
    {
        readonly SemaphoreSlim _semaphore;
        readonly Queue<IRedisCommand> _commandQueue;

        public CommandQueue()
        {
            _commandQueue = new Queue<IRedisCommand>();
            _semaphore = new SemaphoreSlim(1, 1);
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
            await _semaphore.WaitAsync();

            try
            {
                var salvagable = _commandQueue.ToArray();

                Clear();

                await synchronisedWork();

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
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task Enqueue(IRedisCommand command, CancellationToken cancellation = default)
        {
            await _semaphore.WaitAsync(cancellation);

            try
            {
                await command.Execute();

                _commandQueue.Enqueue(command);
            }
            finally
            {
                _semaphore.Release();
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
            await _semaphore.WaitAsync(cancellation);

            try
            {
                return work(_commandQueue);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        void Clear()
        {
            _commandQueue.Clear();
        }
    }
}