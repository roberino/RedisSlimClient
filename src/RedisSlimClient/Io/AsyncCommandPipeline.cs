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

        readonly CancellationTokenSource _cancellation;

        volatile PipelineStatus _status;

        public AsyncCommandPipeline(IDuplexPipeline pipeline, ISocket socket, IWorkScheduler workScheduler, ITelemetryWriter telemetryWriter)
        {
            _pipeline = pipeline;
            _socket = socket;
            _commandQueue = new CommandQueue();
            _cancellation = new CancellationTokenSource();

            new CompletionHandler(_pipeline.Receiver, _commandQueue, workScheduler).AttachTelemetry(telemetryWriter, Severity.Diagnostic);

            var throttledScheduler = new TimeThrottledScheduler(workScheduler, TimeSpan.FromMilliseconds(500));

            _pipeline.Faulted += () =>
            {
                _status = PipelineStatus.Broken;

                telemetryWriter.Execute(ctx =>
                {
                    throttledScheduler.Schedule(Reconnect);
                }, nameof(Reconnect));
            };

            _pipeline.Schedule(workScheduler);

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