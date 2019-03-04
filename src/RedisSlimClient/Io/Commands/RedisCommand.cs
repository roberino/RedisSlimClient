using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    abstract class RedisCommand
    {
        protected RedisCommand(string commandText)
        {
            CommandText = commandText;
        }

        public string CommandText { get; }

        public abstract Task WriteAsync(Func<object[], Task> commandWriter);

        public void Write(Action<object[]> commandWriter)
        {
            WriteAsync(x =>
            {
                commandWriter(x);
                return Task.CompletedTask;
            });
        }

        public Task<object> Result { get; }
    }
}
