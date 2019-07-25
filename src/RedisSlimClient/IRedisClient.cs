using RedisSlimClient.Io.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public interface IRedisReader : IDisposable
    {
        Task<byte[]> GetBytesAsync(string key, CancellationToken cancellation = default);
        Task<string> GetStringAsync(string key, CancellationToken cancellation = default);
        Task<T> GetObjectAsync<T>(string key, CancellationToken cancellation = default);
    }

    public interface IRedisDiagnosticClient : IDisposable
    {
        Task<bool> PingAsync(CancellationToken cancellation = default);
        Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default);
    }

    public interface IRedisReaderWriter : IRedisReader
    {
        Task<long> DeleteAsync(string key, CancellationToken cancellation = default);
        Task<bool> SetBytesAsync(string key, byte[] data, CancellationToken cancellation = default);
        Task<bool> SetStringAsync(string key, string data, CancellationToken cancellation = default);
        Task<bool> SetObjectAsync<T>(string key, T obj, CancellationToken cancellation = default);
    }

    public interface IRedisClient : IRedisReaderWriter, IRedisDiagnosticClient
    {
    }
}