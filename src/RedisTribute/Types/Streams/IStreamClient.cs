using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types.Streams
{
    interface IStreamClient
    {
        Task<StreamId> XAddAsync(RedisKey key, IDictionary<RedisKey, RedisKey> keyValues, CancellationToken cancellation);
    }
}