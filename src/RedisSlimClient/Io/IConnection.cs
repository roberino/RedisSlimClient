using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    internal interface IConnection : IDisposable
    {
        Task<ICommandPipeline> ConnectAsync();
    }
}