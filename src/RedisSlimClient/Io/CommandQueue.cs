﻿using RedisSlimClient.Io.Commands;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class CommandQueue
    {
        readonly SemaphoreSlim _semaphore;
        readonly ConcurrentQueue<IRedisCommand> _commandQueue;

        public CommandQueue()
        {
            _commandQueue = new ConcurrentQueue<IRedisCommand>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public int QueueSize => _commandQueue.Count;

        public void AbortAll(Exception ex)
        {
            while (ProcessNextCommand(cmd =>
             {
                 cmd.Abandon(ex);
             }))
            {
            }
        }

        public async Task Requeue(Func<Task> synchronisedWork)
        {
            await _semaphore.WaitAsync();

            try
            {
                await synchronisedWork();

                var salvagable = _commandQueue.ToArray();

                Clear();

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

        public async Task Enqueue(Func<Task<IRedisCommand>> commandFactory, CancellationToken cancellation = default)
        {
            IRedisCommand cmd;

            await _semaphore.WaitAsync(cancellation);

            try
            {
                cmd = await commandFactory();

                _commandQueue.Enqueue(cmd);
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
            if (_commandQueue.TryDequeue(out var next))
            {
                action(next);

                return true;
            }

            return !_commandQueue.IsEmpty;
        }

        void Clear()
        {
#if NET_CORE
            _commandQueue.Clear();
#else
            while (_commandQueue.Count > 0)
            {
                _commandQueue.TryDequeue(out var result);
            }
#endif
        }
    }
}