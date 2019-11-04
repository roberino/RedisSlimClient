﻿using RedisTribute.Io.Server;
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

    public interface IRedisObjectReaderWriter
    {
        Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default);
        Task<bool> SetAsync<T>(string key, T obj, SetOptions options = default, CancellationToken cancellation = default);
    }

    public interface IHashSetClient
    {
        Task<bool> SetHashField(string key, string field, byte[] data, CancellationToken cancellation = default);
        Task<IDictionary<string, byte[]>> GetAllHashFields(string key, CancellationToken cancellation = default);
        Task<byte[]> GetHashField(string key, string field, CancellationToken cancellation = default);
    }

    public interface IIPersistentDictionaryClient
    {
        Task<IPersistentDictionary<T>> GetHashSet<T>(string key, CancellationToken cancellation = default);
    }

    public interface IRedisClient : IRedisReaderWriter, IRedisObjectReaderWriter, IRedisDiagnosticClient, IHashSetClient, IIPersistentDictionaryClient
    {
    }
}