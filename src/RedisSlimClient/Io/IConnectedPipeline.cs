using System;
using System.Threading.Tasks;
using RedisSlimClient.Io.Server;

namespace RedisSlimClient.Io
{
    interface IConnectedPipeline : IDisposable
    {
        float Workload { get; }

        ServerEndPointInfo EndPointInfo { get; }

        Task<ICommandPipeline> GetPipeline();
    }
}