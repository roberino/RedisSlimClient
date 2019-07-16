using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        string Id { get; }
        ServerEndPointInfo EndPointInfo { get; }
        Task<float> CalculateWorkLoad(ICommandIdentity command);
        Task<ICommandPipeline> RouteCommandAsync(ICommandIdentity command);
    }
}