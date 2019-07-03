using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public interface IRedisClient : IDisposable
    {
        Task<long> DeleteAsync(string key, CancellationToken cancellation = default);
        Task<byte[]> GetDataAsync(string key, CancellationToken cancellation = default);
        Task<T> GetObjectAsync<T>(string key, CancellationToken cancellation = default);
        Task<bool> PingAsync(CancellationToken cancellation = default);
        Task<bool> SetDataAsync(string key, byte[] data, CancellationToken cancellation = default);
        Task<bool> SetObjectAsync<T>(string key, T obj, CancellationToken cancellation = default);
    }
}