using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Threading;
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
            
            _pipeline.Faulted += () =>
            {
                telemetryWriter.Write(new TelemetryEvent()
                {
                     Name = $"{nameof(IDuplexPipeline)}.{nameof(IDuplexPipeline.Faulted)}"
                });

                _commandQueue.AbortAll(new Exception());
                _pipeline.Reset();
            };

            _pipeline.ScheduleOnThreadpool();
        }

        public (int PendingWrites, int PendingReads) PendingWork => ((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public async Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncCommandPipeline));
            }

            command.Execute = async () =>
            {
                await _pipeline.Sender.SendAsync(m =>
                {
                    var formatter = new RedisByteFormatter(m);

                    if (!cancellation.IsCancellationRequested)
                    {
                        return formatter.Write(command.GetArgs());
                    }

                    return Task.CompletedTask;
                });
            };

            _pendingWrites.Increment();

            try
            {
                cancellation.Register(command.Cancel);

                await _commandQueue.Enqueue(command, cancellation);
            }
            catch (Exception ex)
            {
                command.Abandon(ex);
            }
            finally
            {
                _pendingWrites.Decrement();
            }

            return await command;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _pipeline.Dispose();
            }

            _disposed = true;
        }
    }
}