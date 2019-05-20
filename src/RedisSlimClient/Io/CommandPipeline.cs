using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using RedisSlimClient.Serialization;
using System.Collections.Generic;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout);
    }

    internal class CommandPipeline : ICommandPipeline
    {
        readonly Stream _writeStream;
        readonly IEnumerable<RedisObjectPart> _reader;
        readonly CommandQueue _commandQueue;
        readonly IWorkScheduler _scheduler;

        bool _disposed;

        public CommandPipeline(Stream networkStream, IWorkScheduler scheduler = null)
        {
            _writeStream = networkStream;
            _reader = new RedisByteSequenceReader(new StreamIterator(networkStream));
            _commandQueue = new CommandQueue();
            _scheduler = scheduler ?? new WorkScheduler();

            _scheduler.Schedule(ProcessQueue);
        }

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            await _commandQueue.Enqueue(() =>
            {
                command.Write(_writeStream);
                _scheduler.Awake();
                return command;
            }, timeout);

            return await command;
        }

        bool ProcessQueue()
        {
            _writeStream.Flush();

            return _commandQueue.ProcessNextCommand(cmd =>
            {
                cmd.Read(_reader);
            });
        }

        public void Dispose()
        {
            _disposed = true;
            _scheduler.Dispose();
        }
    }
}