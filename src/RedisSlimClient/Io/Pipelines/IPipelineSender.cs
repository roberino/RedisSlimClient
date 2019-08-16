using RedisSlimClient.Types.Primatives;
using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineSender : IPipelineComponent
    {
        ValueTask SendAsync(byte[] data);

        ValueTask SendAsync(Func<IMemoryCursor, ValueTask> writeAction);
    }
}
