using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class AsyncCommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

        readonly IDuplexPipeline _pipeline;
        readonly ITelemetryWriter _telemetryWriter;
        readonly CommandQueue _commandQueue;

        bool _disposed;

        public AsyncCommandPipeline(IDuplexPipeline pipeline, ITelemetryWriter telemetryWriter)
        {
            _pipeline = pipeline;
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _commandQueue = new CommandQueue();
        }

        public (int PendingWrites, int PendingReads) PendingWork => ((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            _pendingWrites.Increment();

            try
            {
                await _commandQueue.Enqueue(async () =>
                {
                    await _pipeline.Sender.SendAsync(m =>
                    {
                        var formatter = new RedisByteFormatter(m);

                        return formatter.Write(command.GetArgs());
                    });

                    return command;
                }, timeout);
            }
            finally
            {
                _pendingWrites.Decrement();
            }

            return await command;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        class PipeAdapter : IRedisObjectWriter
        {
            public PipeAdapter(IPipelineSender sender)
            {

            }

            public Task WriteAsync(IEnumerable<object> objects)
            {
                throw new NotImplementedException();
            }
        }
    }
}