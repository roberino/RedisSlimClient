using RedisTribute.Types.Streams;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Types;

namespace RedisTribute
{
    public interface IRedisStream<T>
    {
        Task<StreamId> WriteAsync(T value, CancellationToken cancellation = default);

        Task<bool> DeleteAsync(CancellationToken cancellation = default);
    }

    public interface IRedisStreamClient
    {
        Task<IRedisStream<T>> GetStream<T>(RedisKey key, CancellationToken cancellation = default);
    }
}
