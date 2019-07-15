using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        string Id { get; }
        float WorkLoad { get; }
        ServerEndPointInfo EndPointInfo { get; }
        Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command);
    }
}