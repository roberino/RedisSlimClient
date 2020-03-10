using System;
using RedisTribute.Types.Streams;

namespace RedisTribute.Types.Pipelines
{
    public readonly struct TransformationResult<TIn, TOut>
    {
        public StreamEntryId Id { get; }
        public TIn Input { get; }
        public TOut Output { get; }
        public Exception Error { get; }

        public TransformationResult(StreamEntryId id, TIn input, TOut output, Exception error)
        {
            Id = id;
            Input = input;
            Output = output;
            Error = error;
        }
    }
}
