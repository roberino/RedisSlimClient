using RedisSlimClient.Io.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface ICommandPipeline : IDisposable
    {
        (int PendingWrites, int PendingReads) PendingWork { get; }

        Task<T> Execute<T>(IRedisResult<T> command, CancellationToken cancellation = default);
    }
}