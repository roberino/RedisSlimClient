using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Streams
{
    interface IPrimativeStreamClient
    {
        Task<StreamId> XAddAsync(RedisKey key, IDictionary<RedisKey, RedisKey> keyValues, CancellationToken cancellation);
    }
}