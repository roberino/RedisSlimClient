using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class CommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

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

        public (int PendingWrites, int PendingReads) PendingWork => ((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            _pendingWrites.Increment();

            await _commandQueue.Enqueue(() =>
            {
                try
                {
                    command.Write(_writeStream);
                    return command;
                }
                finally
                {
                    _pendingWrites.Decrement();
                }
            }, timeout);

            _scheduler.Awake();

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