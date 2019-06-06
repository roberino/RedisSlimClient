using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineSender : IDisposable
    {
        Task SendAsync(byte[] data);
    }
}
