using System;
using System.Collections.Generic;
using RedisTribute.Types.Streams;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types;
using RedisTribute.Types.Pipelines;

namespace RedisTribute
{
    public interface IRedisStream<T>
    {
        Task<StreamEntryId> WriteAsync(T value, CancellationToken cancellation = default);

        Task ReadAllAsync(Func<KeyValuePair<StreamEntryId, T>, Task> processor,
            bool exitWhenNoData = true, int batchSize = 100, CancellationToken cancellation = default);

        Task ReadAsync(Func<KeyValuePair<StreamEntryId, T>, Task> processor, StreamEntryId start,
            StreamEntryId? end = null, bool exitWhenNoData = true, int batchSize = 100,
            CancellationToken cancellation = default);

        Task<bool> DeleteAsync(CancellationToken cancellation = default);
    }

    public interface IRedisStreamClient
    {
        IRedisStream<T> GetStream<T>(RedisKey key, CancellationToken cancellation = default);

        StreamPipeline<TIn> CreatePipeline<TIn>(PipelineOptions options);
    }
}
