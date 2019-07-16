using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Monitoring;
using RedisSlimClient.Io.Net;
using RedisSlimClient.Serialization;
using RedisSlimClient.Serialization.Protocol;
using RedisSlimClient.Types;
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
        readonly Stream _writeStream;
        readonly IManagedSocket _socket;
        readonly IEnumerable<RedisObjectPart> _reader;

        bool _disposed;
        int _pendingWrites;
        int _pendingReads;

        SyncCommandPipeline(Stream networkStream, IManagedSocket socket)
        {
            _writeStream = networkStream;
            _socket = socket;
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));

            Status = PipelineStatus.Uninitialized;
        }

        public static async Task<ICommandPipeline> CreateAsync(IManagedSocket socket)
        {
            var stream = await socket.CreateStream();

            return new SyncCommandPipeline(stream, socket);
        }

        public ConnectionMetrics Metrics => new ConnectionMetrics(_pendingWrites, _pendingReads);

        public PipelineStatus Status { get; private set; }

        public async Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SyncCommandPipeline));
            }

            lock (_writeStream)
            {
                _pendingWrites++;

                try
                {
                    _writeStream.Write(command.GetArgs());
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
                    Status = PipelineStatus.Broken;
                    throw;
                }
                finally
                {
                    _pendingReads--;
                }
            }

            var result = await command;

            Status = PipelineStatus.Ok;

            return result;
        }

        public void Dispose()
        {
            _disposed = true;
            _socket.Dispose();
        }
    }
}