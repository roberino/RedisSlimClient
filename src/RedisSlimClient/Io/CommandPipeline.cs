using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using RedisSlimClient.Serialization;
using System.Collections.Generic;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout);
    }

    internal class CommandPipeline : ICommandPipeline
    {
        readonly Stream _writeStream;
        readonly IEnumerable<RedisObjectPart> _reader;
        readonly CommandQueue _commandQueue;

        bool _disposed;

        public CommandPipeline(Stream networkStream)
        {
            _writeStream = networkStream;
            _reader = new RedisByteSequenceReader(new StreamIterator(networkStream));
            _commandQueue = new CommandQueue();

            Task.Run(() => ProcessQueue());
        }

        public async Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            await _commandQueue.Enqueue(() =>
            {
                command.Write(_writeStream);
                return command;
            }, timeout);

            return await command;
        }

        void ProcessQueue()
        {
            _writeStream.Flush();

            while (!_disposed && !_commandQueue.ProcessNextCommand(cmd =>
            {
                cmd.Read(_reader);
            }))
            {
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}