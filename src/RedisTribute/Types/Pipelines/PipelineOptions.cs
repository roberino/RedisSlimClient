using System;
using RedisTribute.Configuration;
using RedisTribute.Types.Streams;

namespace RedisTribute.Types.Pipelines
{
    public sealed class PipelineOptions
    {
        private PipelineOptions(string ns, StreamEntryId startFrom, bool exitWhenNoData = false)
        {
            Namespace = ns;
            StartFrom = startFrom;
            ExitWhenNoData = exitWhenNoData;
        }

        public static PipelineOptions FromStartOfStream(string streamNamespace, bool exitWhenNoData = false)
            => new PipelineOptions(streamNamespace, StreamEntryId.Start, exitWhenNoData);

        public static PipelineOptions FromNow(string streamNamespace, bool exitWhenNoData = false)
            => new PipelineOptions(streamNamespace, StreamEntryId.FromUtcDateTime(DateTime.UtcNow), exitWhenNoData);

        public static PipelineOptions FromTime(string streamNamespace, DateTime fromTime, bool exitWhenNoData = false)
            => new PipelineOptions(streamNamespace, StreamEntryId.FromUtcDateTime(fromTime), exitWhenNoData);

        public static PipelineOptions FromStreamEntryId(string streamNamespace, StreamEntryId startId, bool exitWhenNoData = false)
            => new PipelineOptions(streamNamespace, startId, exitWhenNoData);

        public string Namespace { get; }

        public StreamEntryId StartFrom { get; }

        public bool ExitWhenNoData { get; }

        public string ResolvePipelineName<T>() => KeySpace.Default.GetStreamKey($"{Namespace}/{typeof(T).Name}");

        internal string ResolvePipelineName<T>(string newNamespace) => KeySpace.Default.GetStreamKey($"{newNamespace}/{typeof(T).Name}");
    }
}
