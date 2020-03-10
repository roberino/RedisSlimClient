using System;
using RedisTribute.Types.Streams;

namespace RedisTribute.Types.Pipelines
{
    public sealed class PipelineOptions
    {
        private PipelineOptions(string ns, StreamEntryId startFrom)
        {
            Namespace = ns;
            StartFrom = startFrom;
        }

        public static PipelineOptions FromStartOfStream(string streamNamespace)
            => new PipelineOptions(streamNamespace, StreamEntryId.Start);

        public static PipelineOptions FromNow(string streamNamespace)
            => new PipelineOptions(streamNamespace, StreamEntryId.FromUtcDateTime(DateTime.UtcNow));

        public static PipelineOptions FromTime(string streamNamespace, DateTime fromTime)
            => new PipelineOptions(streamNamespace, StreamEntryId.FromUtcDateTime(fromTime));

        public static PipelineOptions FromStreamEntryId(string streamNamespace, StreamEntryId startId)
            => new PipelineOptions(streamNamespace, startId);

        public string Namespace { get; }

        public StreamEntryId StartFrom { get; }
    }
}
