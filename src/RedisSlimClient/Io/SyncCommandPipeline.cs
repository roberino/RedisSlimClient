using RedisSlimClient.Io.Commands;
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
        readonly IEnumerable<RedisObjectPart> _reader;

        bool _disposed;
        int _pendingWrites;
        int _pendingReads;

        public SyncCommandPipeline(Stream networkStream)
        {
            _writeStream = networkStream;
            _reader = new ArraySegmentToRedisObjectReader(new StreamIterator(networkStream));
        }

        public (int PendingWrites, int PendingReads) PendingWork => (_pendingWrites, _pendingReads);

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
                finally
                {
                    _pendingReads--;
                }
            }

            return await command;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}