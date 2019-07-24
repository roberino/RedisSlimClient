﻿using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Io.Server;
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
    internal class SyncCommandPipeline : ICommandPipeline
    {
        readonly object _lockObj = new object();
        readonly IManagedSocket _socket;
        readonly IWorkScheduler _scheduler;
        readonly IEnumerable<RedisObjectPart> _reader;

        bool _disposed;
        int _pendingWrites;
        int _pendingReads;

        Stream _writeStream;
        volatile PipelineStatus _status;

        SyncCommandPipeline(Stream networkStream, IManagedSocket socket, IWorkScheduler scheduler = null)
        {
            _writeStream = networkStream;
            _socket = socket;
            _scheduler = new TimeThrottledScheduler(scheduler ?? ThreadPoolScheduler.Instance, TimeSpan.FromMilliseconds(500));
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));

            _status = PipelineStatus.Uninitialized;

            Initialising = new AsyncEvent<ICommandPipeline>();

            _socket.State.Changed += s =>
            {
                if (s == SocketStatus.WriteFault || s == SocketStatus.ReadFault)
                {
                    _status = PipelineStatus.Broken;

                    _scheduler.Schedule(Reconnect);
                }
            };
        }

        public static async Task<ICommandPipeline> CreateAsync(IManagedSocket socket)
        {
            var stream = await socket.CreateStream();

            return new SyncCommandPipeline(stream, socket);
        }


        public ConnectionMetrics Metrics => new ConnectionMetrics(_pendingWrites, _pendingReads);

        public PipelineStatus Status => _status;

        public IAsyncEvent<ICommandPipeline> Initialising { get; }

        public Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation);

        public Task<T> ExecuteAdmin<T>(IRedisResult<T> command, CancellationToken cancellation = default) => ExecuteInternal(command, cancellation, true);

        public void Dispose()
        {
            _disposed = true;
            _socket.Dispose();
        }

        async Task<T> ExecuteInternal<T>(IRedisResult<T> command, CancellationToken cancellation = default, bool isAdmin = false)
        {
            if (_disposed)
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
                _pendingWrites++;

                try
                {
                    command.Execute().GetAwaiter().GetResult();
                }
                finally
                {
                    _pendingWrites--;
                }

                _pendingReads++;

                try
                {
                    command.Complete(_reader.ToObjects().First());
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

        async Task Reconnect()
        {
            if (_status == PipelineStatus.Reinitializing)
            {
                return;
            }

            _status = PipelineStatus.Reinitializing;

            var currentStream = _writeStream;

            _writeStream = null;

            try
            {
                await _socket.ConnectAsync();
                _writeStream = await _socket.CreateStream();

                await ((AsyncEvent<ICommandPipeline>)Initialising).PublishAsync(this);

                _status = PipelineStatus.Ok;
            }
            catch
            {
                _status = PipelineStatus.Broken;
            }

            currentStream?.Dispose();
        }
    }
}