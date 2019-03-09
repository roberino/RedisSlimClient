using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    abstract class RedisCommand
    {
        protected RedisCommand(string commandText)
        {
            CommandText = commandText;
            CompletionSource = new TaskCompletionSource<RedisObject>();
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

        public TaskCompletionSource<RedisObject> CompletionSource { get; }

        public TaskAwaiter<RedisObject> GetAwaiter() => CompletionSource.Task.GetAwaiter();
    }
}
