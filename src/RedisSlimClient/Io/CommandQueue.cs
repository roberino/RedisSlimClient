using RedisSlimClient.Io.Commands;
using System;
using System.Collections.Concurrent;
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

        public async Task Enqueue(Func<Task<IRedisCommand>> commandFactory, TimeSpan timeout)
        {
            IRedisCommand cmd;

            await _semaphore.WaitAsync(timeout);

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

        public bool ProcessNextCommand(Action<IRedisCommand> action)
        {
            if (_commandQueue.TryDequeue(out var next))
            {
                action(next);

                return true;
            }

            return !_commandQueue.IsEmpty;
        }
    }
}