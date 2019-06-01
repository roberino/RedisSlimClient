using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        string Id { get; }
        float WorkLoad { get; }
        Task<ICommandPipeline> ConnectAsync();
    }
}