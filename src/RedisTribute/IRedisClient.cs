using RedisTribute.Io.Server;
using RedisTribute.Types;
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
        Task<IDictionary<string, string>> GetStringsAsync(IReadOnlyCollection<string> keys,
            CancellationToken cancellation = default);
    }

    public interface IRedisDiagnosticClient : IDisposable
    {
        string ClientName { get; }
        Task<bool> PingAsync(CancellationToken cancellation = default);
        Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default);
    }

    public interface IRedisReaderWriter : IRedisReader
    {
        Task<long> ScanKeysAsync(ScanOptions scanOptions, CancellationToken cancellation = default);
        Task<long> DeleteAsync(string key, CancellationToken cancellation = default);
        Task<bool> SetAsync(string key, byte[] data, SetOptions options = default, CancellationToken cancellation = default);
        Task<bool> SetAsync(string key, string data, SetOptions options = default, CancellationToken cancellation = default);
    }

    public interface IRedisMultiReaderWriter
    {
        Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default);
        Task<bool> SetAsync<T>(string key, T obj, SetOptions options = default, CancellationToken cancellation = default);
    }

    public interface IRedisClient : IRedisReaderWriter, IRedisMultiReaderWriter, IRedisDiagnosticClient
    {
    }
}