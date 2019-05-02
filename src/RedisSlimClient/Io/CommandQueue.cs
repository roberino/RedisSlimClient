using RedisSlimClient.Io.Commands;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Types;

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

        public async Task Enqueue(Func<IRedisCommand> commandFactory, TimeSpan timeout)
        {
            IRedisCommand cmd;

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

            await cmd;
        }

        public bool ProcessNextCommand(Action<IRedisCommand> action)
        {
            if (_commandQueue.Count > 0 && _commandQueue.TryDequeue(out var next))
            {
                action(next);

                return true;
            }

            return false;
        }
    }
}