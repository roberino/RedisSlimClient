﻿using RedisSlimClient.Io.Commands;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Io.Types;

namespace RedisSlimClient.Io
{
    class CommandQueue
    {
        readonly SemaphoreSlim _semaphore;
        readonly ConcurrentQueue<RedisCommand> _commandQueue;

        public CommandQueue()
        {
            _commandQueue = new ConcurrentQueue<RedisCommand>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task<RedisObject> Enqueue(Func<RedisCommand> commandFactory, TimeSpan timeout)
        {
            RedisCommand cmd;

            await _semaphore.WaitAsync(timeout);

            try
            {
                cmd = commandFactory();

                _commandQueue.Enqueue(cmd);
            }
            finally
            {
                _semaphore.Release();
            }

            return await cmd;
        }

        public void ProcessNextCommand(Action<RedisCommand> action)
        {
            if (_commandQueue.Count > 0 && _commandQueue.TryDequeue(out var next))
            {
                action(next);
            }
        }
    }
}