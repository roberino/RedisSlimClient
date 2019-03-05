using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RedisSlimClient.Io.Commands;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Io.Types;

namespace RedisSlimClient.Io
{
    interface ICommandPipeline : IDisposable
    {
        Task<RedisObject> Execute(RedisCommand command, TimeSpan timeout);
    }

    class CommandPipeline : ICommandPipeline
    {
        readonly Stream _writeStream;
        readonly DataReader _reader;
        readonly CommandQueue _commandQueue;

        bool _disposed;

        public CommandPipeline(Stream writeStream)
        {
            _writeStream = writeStream;
            _reader = new DataReader(new StreamIterator(writeStream));
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
            while (!_disposed)
            {
                _writeStream.Flush();

                _commandQueue.ProcessNextCommand(cmd =>
                {
                    try
                    {
                        var nextResult = _reader.First();

                        cmd.CompletionSource.SetResult(nextResult);
                    }
                    catch (Exception ex)
                    {
                        cmd.CompletionSource.SetException(ex);
                    }
                });
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}