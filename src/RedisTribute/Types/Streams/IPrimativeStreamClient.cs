using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Streams
{
    interface IPrimativeStreamClient : IRedisKeyManager
    {
        Task<StreamEntryId> XAddAsync(RedisKey key, IDictionary<RedisKey, RedisKey> keyValues, CancellationToken cancellation = default);

        Task<(StreamEntryId id, IDictionary<RedisKey, RedisKey> data)[]> XRange(RedisKey key, StreamEntryId start,
            StreamEntryId end, int? count = null, CancellationToken cancellation = default);
    }
}