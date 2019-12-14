using RedisTribute.Io.Commands;
using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Net;
using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Scheduling;
using RedisTribute.Serialization.Protocol;
using RedisTribute.Telemetry;
using RedisTribute.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class AsyncCommandPipeline : ICommandPipeline
    {
        readonly SyncedCounter _pendingCommands = new SyncedCounter();

        readonly IDuplexPipeline _pipeline;
        readonly ISocket _socket;
        readonly ICommandQueue _commandQueue;

        readonly CancellationTokenSource _cancellation;

        volatile PipelineStatus _status;

        public AsyncCommandPipeline(IDuplexPipeline pipeline, ISocket socket, IWorkScheduler workScheduler, ITelemetryWriter telemetryWriter, ICommandQueue commandQueue = null)
        {
            _pipeline = pipeline;
            _socket = socket;
            _commandQueue = commandQueue ?? new CommandQueue();
            _cancellation = new CancellationTokenSource();

            new CompletionHandler(_pipeline.Receiver, _commandQueue, workScheduler).AttachTelemetry(telemetryWriter, Severity.Diagnostic);

            var throttledScheduler = new TimeThrottledScheduler(ThreadPoolScheduler.Instance, TimeSpan.FromMilliseconds(500));

            _pipeline.Faulted += () =>
            {
                _status = PipelineStatus.Broken;

                telemetryWriter.Execute(ctx =>
                {
                    throttledScheduler.Schedule(Reconnect);
                }, nameof(Reconnect));
            };

            _pipeline.Schedule(ThreadPoolScheduler.Instance);

            _status = PipelineStatus.Uninitialized;
            Initialising = new AsyncEvent<ICommandPipeline>();
        }

        public PipelineStatus Status => _status;

        public PipelineMetrics Metrics => new PipelineMetrics((int)_pendingCommands.Value, _commandQueue.QueueSize);

        public IAsyncEvent<ICommandPipeline> Initialising { get; }

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation);

        public Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation, true);

        public void Dispose()
        {
            if (!_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
                _pipeline.Dispose();
                _commandQueue.Dispose();
            }
        }

        async Task<T> ExecuteInternal<T>(IRedisResult<T> command, CancellationToken cancellation = default, bool isAdmin = false)
        {
            if (_cancellation.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(AsyncCommandPipeline));
            }

            cancellation.ThrowIfCancellationRequested();

            if (Status != PipelineStatus.Ok && !isAdmin)
            {
                command.Abandon(new ConnectionUnavailableException(_socket.EndpointIdentifier));

                return await command;
            }

            command.AssignedEndpoint = _socket.EndpointIdentifier;

            command.OnExecute = async (args) =>
            {
                await _pipeline.Sender.SendAsync(m =>
                {
                    var formatter = new RedisByteFormatter(m);

                    return formatter.Write(args);
                }, cancellation);
            };

            _pendingCommands.Increment();

            try
            {
                try
                {
                    cancellation.Register(command.Cancel);

                    await _commandQueue.Enqueue(command, cancellation);
                }
                catch (Exception ex)
                {
                    command.Abandon(ex);
                }

                var result = await command;

                _status = PipelineStatus.Ok;

                return result;
            }
            finally
            {
                _pendingCommands.Decrement();
            }
        }

        async Task Reconnect()
        {
            if (_status == PipelineStatus.Reinitializing || _cancellation.IsCancellationRequested)
            {
                return;
            }

            _status = PipelineStatus.Reinitializing;

            await Attempt.WithExponentialBackoff(async () =>
            {
                await _commandQueue.Requeue(async () =>
                {
                    await _pipeline.ResetAsync();

                    await ((AsyncEvent<ICommandPipeline>)Initialising).PublishAsync(this);
                });
            }, TimeSpan.FromSeconds(5), cancellation: _cancellation.Token);

            _status = PipelineStatus.Ok;
        }
    }
}