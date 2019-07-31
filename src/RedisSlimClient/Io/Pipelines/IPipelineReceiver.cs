using System;
using System.Buffers;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineReceiver : IPipelineComponent
    {
        void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimiter, Action<ReadOnlySequence<byte>> handler);
    }
}