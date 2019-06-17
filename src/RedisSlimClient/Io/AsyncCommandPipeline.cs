using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Types;
using RedisSlimClient.Util;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal class AsyncCommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

        readonly IDuplexPipeline _pipeline;
        readonly ITelemetryWriter _telemetryWriter;
        readonly CommandQueue _commandQueue;
        readonly CompletionHandler _completionHandler;

        bool _disposed;

        public AsyncCommandPipeline(IDuplexPipeline pipeline, ITelemetryWriter telemetryWriter)
        {
            _pipeline = pipeline;
            _telemetryWriter = telemetryWriter ?? NullWriter.Instance;
            _commandQueue = new CommandQueue();
            _completionHandler = new CompletionHandler(_pipeline.Receiver, _commandQueue);

            _pipeline.ScheduleOnThreadpool();
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

            command.Complete(new RedisString(new byte[0]));

            return await command;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                _pipeline.Dispose();
            }

            _disposed = true;
        }
    }
}