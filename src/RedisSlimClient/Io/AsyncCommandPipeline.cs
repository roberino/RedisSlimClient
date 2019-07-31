using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Pipelines;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Telemetry;
using RedisSlimClient.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class AsyncCommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingWrites = new SyncedCounter();

        readonly IDuplexPipeline _pipeline;
        readonly ISocket _socket;
        readonly CommandQueue _commandQueue;

        bool _disposed;

        volatile PipelineStatus _status;
        volatile int _reconnectAttempts;

        public AsyncCommandPipeline(IDuplexPipeline pipeline, ISocket socket, IWorkScheduler workScheduler, ITelemetryWriter telemetryWriter)
        {
            _pipeline = pipeline;
            _socket = socket;
            _commandQueue = new CommandQueue();

            var _ = new CompletionHandler(_pipeline.Receiver, _commandQueue, workScheduler);

            var throttledScheduler = new TimeThrottledScheduler(workScheduler, TimeSpan.FromMilliseconds(500));

            _pipeline.Faulted += () =>
            {
                _status = PipelineStatus.Broken;

                telemetryWriter.Execute(ctx =>
                {
                    throttledScheduler.Schedule(Reconnect);
                }, nameof(IDuplexPipeline.Faulted));
            };

            workScheduler.Schedule(_pipeline.RunAsync);

            _status = PipelineStatus.Uninitialized;
            Initialising = new AsyncEvent<ICommandPipeline>();
        }

        public PipelineStatus Status => _status;

        public PipelineMetrics Metrics => new PipelineMetrics((int)_pendingWrites.Value, _commandQueue.QueueSize);

        public IAsyncEvent<ICommandPipeline> Initialising { get; }

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation);

        public Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation, true);

        public void Dispose()
        {
            if (!_disposed)
            {
                _pipeline.Dispose();
            }

            _disposed = true;
        }

        async Task<T> ExecuteInternal<T>(IRedisResult<T> command, CancellationToken cancellation = default, bool isAdmin = false)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AsyncCommandPipeline));
            }

            if (Status != PipelineStatus.Ok && !isAdmin)
            {
                command.Abandon(new ConnectionUnavailableException());

                return await command;
            }

            command.AssignedEndpoint = _socket.EndpointIdentifier;

            command.OnExecute = async (args) =>
            {
                await _pipeline.Sender.SendAsync(m =>
                {
                    var formatter = new RedisByteFormatter(m);

                    return formatter.Write(args);
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

            var result = await command;

            _status = PipelineStatus.Ok;

            return result;
        }

        async Task Reconnect()
        {
            if (_status == PipelineStatus.Reinitializing || _disposed || _reconnectAttempts > 10)
            {
                return;
            }

            _status = PipelineStatus.Reinitializing;
            _reconnectAttempts++;

            try
            {
                await _commandQueue.Requeue(async () =>
                {
                    await _pipeline.Reset();

                    await ((AsyncEvent<ICommandPipeline>)Initialising).PublishAsync(this);
                });

                _reconnectAttempts = 0;
                _status = PipelineStatus.Ok;
            }
            catch
            {
                _status = PipelineStatus.Broken;
            }
        }
    }
}