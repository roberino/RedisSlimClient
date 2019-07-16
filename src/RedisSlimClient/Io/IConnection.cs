using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        string Id { get; }
        Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command);
    }
}