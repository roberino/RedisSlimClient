using System;
using System.Buffers;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineReceiver : IPipelineComponent
    {
        void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, Action<ReadOnlySequence<byte>> handler);
    }
}