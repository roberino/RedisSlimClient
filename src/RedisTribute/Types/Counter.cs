using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types
{
    class Counter : ICounter
    {
        readonly IRedisReaderWriter _client;

        public Counter(RedisKey key, IRedisReaderWriter client)
        {
            Key = key;
            _client = client;
        }

        public RedisKey Key { get; }

        public async Task<long> ReadAsync(CancellationToken cancellation) => await _client.GetLongAsync(Key.ToString(), cancellation);

        public Task<long> IncrementAsync(CancellationToken cancellation) => _client.IncrementAsync(Key.ToString(), cancellation);
    }
}
