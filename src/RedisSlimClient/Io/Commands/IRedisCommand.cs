using RedisSlimClient.Types;
using System.Collections.Generic;
using System.IO;
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
        void Write(Stream commandWriter);
        TaskAwaiter GetAwaiter();
    }
}