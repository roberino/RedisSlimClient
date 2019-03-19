using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.IO;
using System.Threading.Tasks;
using RedisSlimClient.Serialization;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        Task<RedisObject> Execute(RedisCommand command, TimeSpan timeout);
    }

    internal class CommandPipeline : ICommandPipeline
    {
        readonly Stream _writeStream;
        readonly ByteReader _reader;
        readonly CommandQueue _commandQueue;

        bool _disposed;

        public CommandPipeline(Stream writeStream)
        {
            _writeStream = writeStream;
            _reader = new ByteReader(new StreamIterator(writeStream));
            _commandQueue = new CommandQueue();

            Task.Run(() => ProcessQueue());
        }

        public async Task<RedisObject> Execute(RedisCommand command, TimeSpan timeout)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CommandPipeline));
            }

            return await _commandQueue.Enqueue(() =>
            {
                command.Write(x => _writeStream.Write(x));
                return command;
            }, timeout);
        }

        void ProcessQueue()
        {
            _writeStream.Flush();

            foreach (var nextResult in _reader.ToObjects())
            {
                while (!_commandQueue.ProcessNextCommand(cmd =>
                {
                    try
                    {
                        cmd.CompletionSource.SetResult(nextResult);
                    }
                    catch (Exception ex)
                    {
                        cmd.CompletionSource.SetException(ex);
                    }
                }))
                {
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}