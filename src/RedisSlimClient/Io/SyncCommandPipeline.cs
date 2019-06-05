using RedisSlimClient.Io.Commands;
using RedisSlimClient.Serialization;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            lock (_writeStream)
            {
                _pendingWrites++;

                try
                {
                    command.Write(_writeStream);
                }
                finally
                {
                    _pendingWrites--;
                }

                _pendingReads++;

                try
                {
                    command.Read(_reader);
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