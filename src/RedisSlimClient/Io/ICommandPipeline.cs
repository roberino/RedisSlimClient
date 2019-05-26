using RedisSlimClient.Io.Commands;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        Task<T> Execute<T>(IRedisResult<T> command, TimeSpan timeout);
    }
}