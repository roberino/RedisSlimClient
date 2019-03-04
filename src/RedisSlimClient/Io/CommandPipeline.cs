using System.Collections.Generic;
using RedisSlimClient.Io.Commands;
using System.IO;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    interface ICommandPipeline
    {
        Task<IEnumerable<object>> Execute(RedisCommand command);
    }

    class CommandPipeline : ICommandPipeline
    {
        readonly Stream _writeStream;
        readonly DataReader _reader;

        public CommandPipeline(Stream writeStream)
        {
            _writeStream = writeStream;
            _reader = new DataReader(new StreamIterator(writeStream));
        }

        public Task<IEnumerable<object>> Execute(RedisCommand command)
        {
            command.Write(x => _writeStream.Write(x));

            _writeStream.Flush();

            return ReadAsync();
        }

        async Task<IEnumerable<object>> ReadAsync()
        {
            await Task.CompletedTask;

            return _reader;
        }
    }
}
