using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
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
    class SyncCommandPipeline : ICommandPipeline
    {
        readonly object _lockObj = new object();
        readonly IManagedSocket _socket;
        readonly IWorkScheduler _scheduler;
        readonly IEnumerable<RedisObjectPart> _reader;
        readonly CancellationTokenSource _cancellation;

        int _pendingWrites;
        int _pendingReads;

        Stream _writeStream;

        volatile PipelineStatus _status;

        SyncCommandPipeline(Stream networkStream, IManagedSocket socket, IWorkScheduler scheduler = null)
        {
            _cancellation = new CancellationTokenSource();
            _writeStream = networkStream;
            _socket = socket;
            _scheduler = new TimeThrottledScheduler(scheduler ?? ThreadPoolScheduler.Instance, TimeSpan.FromMilliseconds(500));
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));

            _status = PipelineStatus.Uninitialized;

            Initialising = new AsyncEvent<ICommandPipeline>();

            _socket.State.Changed += x =>
            {
                if (x.Status == SocketStatus.WriteFault || x.Status == SocketStatus.ReadFault)
                {
                    _status = PipelineStatus.Broken;

                    _scheduler.Schedule(() => Reconnect(x.Id));
                }
            };
        }

        public static async Task<ICommandPipeline> CreateAsync(IManagedSocket socket)
        {
            var stream = await socket.CreateStream();

            return new SyncCommandPipeline(stream, socket);
        }

        public PipelineMetrics Metrics => new PipelineMetrics(_pendingWrites, _pendingReads);

        public PipelineStatus Status => _status;

        public IAsyncEvent<ICommandPipeline> Initialising { get; }

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation);

        public Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation, true);

        public void Dispose()
        {
            if (!_cancellation.IsCancellationRequested)
            {
                _cancellation.Cancel();
                _socket.Dispose();
                _cancellation.Dispose();
            }
        }

        async Task<T> ExecuteInternal<T>(IRedisResult<T> command, CancellationToken cancellation = default, bool isAdmin = false)
        {
            if (_cancellation.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(SyncCommandPipeline));
            }

            if (_writeStream == null)
            {
                throw new ConnectionUnavailableException();
            }

            if (_status != PipelineStatus.Ok && !isAdmin)
            {
                throw new ConnectionUnavailableException();
            }

            command.AssignedEndpoint = _socket.EndpointIdentifier;

            command.OnExecute = args =>
            {
                _writeStream.Write(args);
                return Task.CompletedTask;
            };

            lock (_lockObj)
            {
                cancellation.ThrowIfCancellationRequested();

                _pendingWrites++;

                try
                {
                    command.Execute().GetAwaiter().GetResult();
                }
                finally
                {
                    _pendingWrites--;
                }

                cancellation.ThrowIfCancellationRequested();

                _pendingReads++;

                try
                {
                    var redisResult = _reader.ToObjects().First();

                    cancellation.ThrowIfCancellationRequested();

                    command.Complete(redisResult);
                }
                catch
                {
                    _status = PipelineStatus.Broken;
                    throw;
                }
                finally
                {
                    _pendingReads--;
                }
            }

            var result = await command;

            _status = PipelineStatus.Ok;

            return result;
        }

        async Task Reconnect(long sequence)
        {
            if (_status == PipelineStatus.Reinitializing || _cancellation.IsCancellationRequested || _socket.State.Sequence > sequence)
            {
                return;
            }

            _status = PipelineStatus.Reinitializing;

            var currentStream = _writeStream;

            currentStream?.Dispose();

            _writeStream = null;

            await Attempt.WithExponentialBackoff(async () =>
            {

                await _socket.ConnectAsync();
                _writeStream = await _socket.CreateStream();

                await ((AsyncEvent<ICommandPipeline>)Initialising).PublishAsync(this);
            }, TimeSpan.FromSeconds(5), cancellation: _cancellation.Token);

            _status = PipelineStatus.Ok;
        }
    }
}