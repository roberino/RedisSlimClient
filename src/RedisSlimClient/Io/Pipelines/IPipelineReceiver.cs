using System;
using System.Buffers;

namespace RedisSlimClient.Io.Pipelines
{
    interface IPipelineReceiver : IDisposable
    {
        event Action<Exception> Error;

        void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, Action<ReadOnlySequence<byte>> handler);
    }
}