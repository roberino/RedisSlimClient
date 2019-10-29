using RedisTribute.Types.Primatives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    interface IPipelineSender : IPipelineComponent
    {
        ValueTask SendAsync(byte[] data);

        ValueTask SendAsync(Func<IMemoryCursor, ValueTask> writeAction, CancellationToken cancellationToken = default);
    }
}
