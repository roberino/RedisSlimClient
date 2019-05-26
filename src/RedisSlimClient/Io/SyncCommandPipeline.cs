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

        public SyncCommandPipeline(Stream networkStream)
        {
            _writeStream = networkStream;
            _reader = new RedisByteSequenceReader(new StreamIterator(networkStream));
        }

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            lock (_writeStream)
            {
                command.Write(_writeStream);
                command.Read(_reader);
            }

            return await command;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}