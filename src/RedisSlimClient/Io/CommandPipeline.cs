using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Types;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class CommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

        readonly IRedisObjectWriter _writeStream;
        readonly ITelemetryWriter _telemetryWriter;
        readonly IEnumerable<RedisObjectPart> _reader;
        readonly CommandQueue _commandQueue;
        readonly IWorkScheduler _scheduler;

        bool _disposed;

        public CommandPipeline(Stream networkStream, ITelemetryWriter telemetryWriter, IWorkScheduler scheduler = null)
        {
            _writeStream = new StreamAdapter(networkStream);
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));
            _commandQueue = new CommandQueue();
            _scheduler = scheduler ?? new WorkScheduler(_telemetryWriter);

            _scheduler.Schedule(ProcessQueue);
        }

        public (int PendingWrites, int PendingReads) PendingWork => ((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            return _telemetryWriter.ExecuteAsync(async ctx =>
            {
                _pendingWrites.Increment();

                await _commandQueue.Enqueue(async () =>
                {
                    try
                    {
                        ctx.Write(nameof(_writeStream.WriteAsync));

                        await _writeStream.WriteAsync(command.GetArgs());

                        return command;
                    }
                    finally
                    {
                        _pendingWrites.Decrement();
                    }
                }, cancellation);

                ctx.Write(nameof(_scheduler.Awake));

                _scheduler.Awake();

                return await command;
            }, nameof(Execute));
        }

        public void Dispose()
        {
            _disposed = true;
            _scheduler.Dispose();
        }

        bool ProcessQueue()
        {
            return _commandQueue.ProcessNextCommand(cmd =>
            {
                cmd.Complete(_reader.ToObjects().First());
            });
        }
    }
}