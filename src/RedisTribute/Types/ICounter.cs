using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Types
{
    public interface ICounter
    {
        RedisKey Key { get; }

        Task<long> IncrementAsync(CancellationToken cancellation = default);
        Task<long> ReadAsync(CancellationToken cancellation = default);
    }
}