using System;
using System.Buffers;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineReceiver : IDisposable
    {
        event Action<Exception> OnException;
        event Action<ReadOnlySequence<byte>> OnRead;

        Task RunAsync();
    }
}