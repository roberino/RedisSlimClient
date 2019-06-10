using RedisSlimClient.Types;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RedisSlimClient.Io.Commands
{
    interface IRedisResult<T> : IRedisCommand
    {
        new TaskAwaiter<T> GetAwaiter();
    }

    interface IRedisCommand
    {
        string CommandText { get; }
        void Read(IEnumerable<RedisObjectPart> objectParts);
        object[] GetArgs();
        TaskAwaiter GetAwaiter();
    }
}