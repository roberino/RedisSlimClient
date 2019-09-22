using RedisTribute.Io.Server;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    public interface IRedisReader : IDisposable
    {
        Task<byte[]> GetAsync(string key, CancellationToken cancellation = default);
        Task<string> GetStringAsync(string key, CancellationToken cancellation = default);
        Task<IReadOnlyCollection<string>> GetStringsAsync(IReadOnlyCollection<string> keys,
            CancellationToken cancellation = default);
    }

    public interface IRedisDiagnosticClient : IDisposable
    {
        Task<bool> PingAsync(CancellationToken cancellation = default);
        Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default);
    }

    public interface IRedisReaderWriter : IRedisReader
    {
        Task<long> DeleteAsync(string key, CancellationToken cancellation = default);
        Task<bool> SetAsync(string key, byte[] data, CancellationToken cancellation = default);
        Task<bool> SetAsync(string key, string data, CancellationToken cancellation = default);
    }

    public interface IRedisMultiReaderWriter
    {
        Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default);
        Task<bool> SetAsync<T>(string key, T obj, CancellationToken cancellation = default);
    }

    public interface IRedisClient : IRedisReaderWriter, IRedisMultiReaderWriter, IRedisDiagnosticClient
    {
    }
}