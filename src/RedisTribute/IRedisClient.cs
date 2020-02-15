using RedisTribute.Configuration;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using RedisTribute.Types.Geo;
using RedisTribute.Types.Graphs;
using RedisTribute.Types.Messaging;
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
        Task<bool> SetAsync(string key, long data, SetOptions options = default, CancellationToken cancellation = default);
        Task<long> GetLongAsync(string key, CancellationToken cancellation = default);
        Task<long> IncrementAsync(string key, CancellationToken cancellation = default);
        Task<ICounter> GetCounter(string key, CancellationToken cancellation = default);
    }

    public interface IRedisObjectReaderWriter
    {
        Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default);
        Task<Result<T>> GetAsync<T>(string key, T defaultValue, CancellationToken cancellation = default);
        Task<bool> SetAsync<T>(string key, T obj, SetOptions options = default, CancellationToken cancellation = default);
    }

    public interface IHashSetClient
    {
        Task<bool> SetHashFieldAsync(string key, string field, byte[] data, CancellationToken cancellation = default);
        Task<IDictionary<string, byte[]>> GetAllHashFieldsAsync(string key, CancellationToken cancellation = default);
        Task<byte[]> GetHashFieldAsync(string key, string field, CancellationToken cancellation = default);
    }

    public interface IPersistentDictionaryProvider
    {
        Task<IPersistentDictionary<T>> GetHashSetAsync<T>(string key, CancellationToken cancellation = default);
    }

    public interface IGraphClient
    {
        IGraph<T> GetGraph<T>(string graphNamespace);
    }

    public interface IGeoClient
    {
        Task<int> GeoAddAsync(string key, GeoMember member, CancellationToken cancellation = default);
        Task<int> GeoAddAsync(string key, GeoMember[] members, CancellationToken cancellation = default);
        Task<double> GeoDistAsync(string key, string member1, string member2, DistanceUnit unit = default, CancellationToken cancellation = default);
        Task<IDictionary<string, string>> GeoHashAsync(string key, string[] members, CancellationToken cancellation = default);
        Task<IDictionary<string, GeoCoordinates>> GeoPosAsync(string key, string[] members, CancellationToken cancellation = default);
        Task<IDictionary<string, GeoDescriptor>> GeoRadiusAsync(GeoRadiusQuery query, CancellationToken cancellation = default);
    }

    public interface ISubscriptionClient : IRedisDiagnosticClient
    {
        Task<ISubscription> SubscribeAsync<T>(string[] channels, Func<IMessage<T>, Task> handler, CancellationToken cancellation = default);
        Task<ISubscription> SubscribeAsync(string[] channels, Func<IMessageData, Task> handler, CancellationToken cancellation = default);
    }

    public interface IPublisherClient
    {
        Task<int> PublishAsync(IMessageData message, CancellationToken cancellation = default);
        Task<int> PublishAsync(Func<ISerializerSettings, IMessageData> messageFactory, CancellationToken cancellation = default);
    }

    public interface IRedisClient : 
        IRedisReaderWriter, 
        IRedisObjectReaderWriter, 
        IRedisDiagnosticClient, 
        IHashSetClient, 
        IPersistentDictionaryProvider,
        IRedLock,
        IGraphClient,
        IPublisherClient,
        IGeoClient
    {
    }
}