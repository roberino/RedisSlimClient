using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RedisSlimClient.Serialization;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisCommand : IRedisResult<RedisObject>
    {
        protected RedisCommand(string commandText)
        {
            CommandText = commandText;
            CompletionSource = new TaskCompletionSource<RedisObject>();
        }

        public string CommandText { get; }

        public virtual object[] GetArgs() => new[] { CommandText };

        public void Write(Stream commandWriter)
        {
            commandWriter.Write(GetArgs());
        }

        public virtual void Read(IEnumerable<RedisObjectPart> objectParts)
        {
            try
            {
                var nextResult = objectParts.ToObjects().First();

                CompletionSource.SetResult(nextResult);
            }
            catch (Exception ex)
            {
                CompletionSource.SetException(ex);
            }
        }

        public TaskCompletionSource<RedisObject> CompletionSource { get; }

        public TaskAwaiter<RedisObject> GetAwaiter() => CompletionSource.Task.GetAwaiter();

        TaskAwaiter IRedisCommand.GetAwaiter() => ((Task)CompletionSource.Task).GetAwaiter();
    }
}
