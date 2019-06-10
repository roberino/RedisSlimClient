using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineSender : IDisposable
    {
        Task SendAsync(byte[] data);

        Task SendAsync(Func<Memory<byte>, int> writeAction, int bufferSize = 512);
    }
}
