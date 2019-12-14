using RedisTribute.Io.Commands;
using RedisTribute.Io.Monitoring;
using RedisTribute.Io.Net;
using RedisTribute.Io.Scheduling;
using RedisTribute.Serialization;
using RedisTribute.Serialization.Protocol;
using RedisTribute.Types;
using RedisTribute.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io
{
    class SyncCommandPipeline : ICommandPipeline
    {
        readonly IManagedSocket _socket;
        readonly IWorkScheduler _scheduler;
        readonly CancellationTokenSource _cancellation;
        readonly AsyncLock _lock;

        int _pendingWrites;
        int _pendingReads;

        Stream _writeStream;
        IEnumerable<RedisObjectPart> _reader;

        volatile PipelineStatus _status;

        SyncCommandPipeline(Stream networkStream, IManagedSocket socket, IWorkScheduler scheduler = null)
        {
            _cancellation = new CancellationTokenSource();
            _writeStream = networkStream;
            _socket = socket;
            _scheduler = new TimeThrottledScheduler(scheduler ?? ThreadPoolScheduler.Instance, TimeSpan.FromMilliseconds(500));
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));
            _lock = new AsyncLock();

            _status = PipelineStatus.Uninitialized;

            Initialising = new AsyncEvent<ICommandPipeline>();

            _socket.State.Changed += x =>
            {
                if (x.Status == SocketStatus.WriteFault || x.Status == SocketStatus.ReadFault)
                {
                    BeginReconnect(x);
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

            command.AssignedEndpoint = _socket.EndpointIdentifier;

            command.OnExecute = args =>
            {
                _writeStream.Write(args);
                return Task.CompletedTask;
            };

            while (_status == PipelineStatus.Broken || (_status == PipelineStatus.Reinitializing && !isAdmin))
            {
                await Task.Delay(10, cancellation);
            }

            Action postActions = () => { };

            using (await _lock.LockAsync(cancellation))
            {
                if (_writeStream == null || (_status != PipelineStatus.Ok && !isAdmin))
                {
                    throw new ConnectionUnavailableException(_socket.EndpointIdentifier);
                }

                _pendingWrites++;

                try
                {
                    await command.Execute();
                }
                catch (IOException ex)
                {
                    postActions = () =>
                    {
                        _socket.State.WriteError(ex);
                        throw ex;
                    };
                }
                finally
                {
                    _pendingWrites--;
                }

                cancellation.ThrowIfCancellationRequested();

                _pendingReads++;

                try
                {
                    foreach (var redisResult in _reader.ToObjects())
                    {
                        if (redisResult == null)
                        {
                            throw new IOException("No data received");
                        }

                        cancellation.ThrowIfCancellationRequested();

                        if (command.SetResult(redisResult))
                        {
                            break;
                        }
                    }
                }
                catch (IOException ex)
                {
                    postActions = () =>
                    {
                        _socket.State.ReadError(ex);
                        throw ex;
                    };
                }
                finally
                {
                    _pendingReads--;
                }
            }

            postActions();

            var result = await command;

            _status = PipelineStatus.Ok;

            return result;
        }

        private void BeginReconnect((SocketStatus Status, long Id) x)
        {
            _status = PipelineStatus.Broken;

            _scheduler.Schedule(() => Reconnect(x.Id));
        }

        async Task Reconnect(long sequence)
        {
            if (_status == PipelineStatus.Reinitializing || _cancellation.IsCancellationRequested || _socket.State.Sequence > sequence)
            {
                return;
            }

            _status = PipelineStatus.Reinitializing;

            var currentStream = _writeStream;

            _writeStream = null;
            _reader = null;

            currentStream?.Dispose();

            await Attempt.WithExponentialBackoff(async () =>
            {
                using (await _lock.LockAsync())
                {
                    await _socket.ConnectAsync();
                    _writeStream = await _socket.CreateStream();
                    _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(_writeStream));
                }

                await ((AsyncEvent<ICommandPipeline>)Initialising).PublishAsync(this);
            }, TimeSpan.FromSeconds(5), cancellation: _cancellation.Token);

            _status = PipelineStatus.Ok;
        }
    }
}