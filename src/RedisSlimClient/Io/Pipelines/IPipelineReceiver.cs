using System;
using System.Buffers;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineReceiver : IDisposable
    {
        event Action<Exception> Error;
        event Action<ReadOnlySequence<byte>> Received;
    }
}