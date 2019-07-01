using RedisSlimClient.Types.Primatives;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineSender : IDisposable
    {
        Task SendAsync(byte[] data);

        Task SendAsync(Func<IMemoryCursor, Task> writeAction);
    }
}
