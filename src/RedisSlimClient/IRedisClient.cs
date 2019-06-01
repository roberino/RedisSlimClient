using System;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public interface IRedisClient : IDisposable
    {
        Task<long> DeleteAsync(string key);
        Task<byte[]> GetDataAsync(string key);
        Task<T> GetObjectAsync<T>(string key);
        Task<bool> PingAsync();
        Task<bool> SetDataAsync(string key, byte[] data);
        Task<bool> SetObjectAsync<T>(string key, T obj);
    }
}