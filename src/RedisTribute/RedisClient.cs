using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Commands.Geo;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using RedisTribute.Types.Geo;
using RedisTribute.Types.Graphs;
using RedisTribute.Types.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisClient : IRedisClient
    {
        readonly RedisController _controller;
        readonly RedisLock _redisLock;

        internal RedisClient(RedisController controller)
        {
            _controller = controller;
            _redisLock = new RedisLock(_controller);
        }

        internal static IRedisClient Create(ClientConfiguration configuration, Action onDisposing = null) =>
            new RedisClient(new RedisController(configuration, e => new ConnectionFactory().Create(e), onDisposing));

        public string ClientName => _controller.Configuration.ClientName;

        public Task<bool> PingAsync(CancellationToken cancellation = default) => _controller.GetResponse(new PingCommand(), cancellation);

        public Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default)
            => _controller.GetResponses(() => new PingCommand(),
                (c, r, m) => new PingResponse(c.AssignedEndpoint, r, ((PingCommand)c).Elapsed, m),
                (c, e, m) => new PingResponse(c.AssignedEndpoint, e, ((PingCommand)c).Elapsed, m), ConnectionTarget.AllNodes);

        public Task<long> DeleteAsync(string key, CancellationToken cancellation = default) => _controller.GetNumericResponse(new DeleteCommand(key), cancellation);

        public Task<bool> SetAsync(string key, byte[] data, SetOptions options = default, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, data, options), cancellation);

        public Task<bool> SetAsync(string key, string data, SetOptions options = default, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, _controller.Configuration.Encoding.GetBytes(data), options), cancellation);

        public Task<bool> SetAsync(string key, long data, SetOptions options = default, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, Encoding.ASCII.GetBytes(data.ToString()), options), cancellation);

        public Task<long> IncrementAsync(string key, CancellationToken cancellation = default) => _controller.GetResponse(new IncrCommand(key), cancellation);

        public Task<bool> SetAsync<T>(string key, T obj, SetOptions options = default, CancellationToken cancellation = default)
            => _controller.GetResponse(new ObjectSetCommand<T>(key, _controller.Configuration, obj, options), cancellation);

        public Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default)
            => Result<T>.FromOperation(() => GetInternalAsync<T>(key, cancellation), cancellation);

        public async Task<Result<T>> GetAsync<T>(string key, T defaultValue, CancellationToken cancellation = default)
        {
            var result = await GetAsync<T>(key, cancellation);

            return result.ResolveNotFound(() => defaultValue);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken cancellation = default)
            => _controller.GetResponse(() => new GetCommand(key), cancellation, ResultConvertion.AsBytes);

        public Task<string> GetStringAsync(string key, CancellationToken cancellation = default)
            => _controller.GetResponse(() => new GetCommand(key), cancellation, ResultConvertion.AsString);

        public Task<long> GetLongAsync(string key, CancellationToken cancellation = default)
            => _controller.GetResponse(() => new GetCommand(key), cancellation, ResultConvertion.AsLong);

        public Task<int> GeoAddAsync(string key, GeoMembers members, CancellationToken cancellation = default)
            => _controller.GetResponse(new GeoAddCommand(new GeoEntity(key, members)), cancellation);

        public Task<int> GeoAddAsync(string key, GeoMember member, CancellationToken cancellation = default)
            => _controller.GetResponse(new GeoAddCommand(new GeoEntity(key, member)), cancellation);

        public Task<double> GeoDistAsync(string key, string member1, string member2, DistanceUnit unit = default, CancellationToken cancellation = default)
            => _controller.GetResponse(new GeoDistCommand(key, member1, member2, unit), cancellation);

        public async Task<IDictionary<string, string>> GeoHashAsync(string key, string[] members, CancellationToken cancellation = default)
        {
            var results = await _controller.GetResponse(new GeoHashCommand(key, members.Select(m => (RedisKey)m).ToArray()), cancellation);

            return results.ToDictionary(r => r.Key.ToString(), r => r.Value);
        }

        public async Task<IDictionary<string, GeoDescriptor>> GeoRadiusAsync(GeoRadiusQuery query, CancellationToken cancellation = default)
        {
            var results = await _controller.GetResponse(new GeoRadiusCommand(query), cancellation);

            return results.ToDictionary(r => r.Key.ToString(), r => r.Value);
        }

        public async Task<IDictionary<string, GeoCoordinates>> GeoPosAsync(string key, string[] members, CancellationToken cancellation = default)
        {
            var results = await _controller.GetResponse(new GeoPosCommand(key, members.Select(m => (RedisKey)m).ToArray()), cancellation);

            return results.ToDictionary(r => r.Key.ToString(), r => r.Value);
        }

        public async Task<ICounter> GetCounter(string key, CancellationToken cancellation = default)
        {
            var qkey = KeySpace.Default.GetCounterKey(key);

            await SetAsync(qkey, 0L, new SetOptions(Expiry.Infinite, SetCondition.SetKeyIfNotExists));

            return new Counter(qkey, this);
        }

        public async Task<IDictionary<string, string>> GetStringsAsync(IReadOnlyCollection<string> keys, CancellationToken cancellation = default)
        {
            var cmd = new MGetCommand(RedisKeys.FromStrings(keys));

            var results = await _controller.GetMultikeyResultAsync(keys, k => new MGetCommand(k), cancellation);

            var resultsTransformed = new Dictionary<string, string>(keys.Count);

            foreach (var result in results)
            {
                foreach (var item in result.Keys.Zip(result.Result, (k, r) => (k, r)))
                {
                    using (item.r)
                    {
                        resultsTransformed[item.k.ToString()] = item.r.ToString(_controller.Configuration.Encoding);
                    }
                }
            }

            return resultsTransformed;
        }

        public Task<int> PublishAsync(IMessageData message, CancellationToken cancellation = default)
            => _controller.GetResponse(new PublishCommand(message), cancellation);

        public Task<int> PublishAsync(Func<ISerializerSettings, IMessageData> messageFactory, CancellationToken cancellation = default)
            => _controller.GetResponse(new PublishCommand(messageFactory(_controller.Configuration)), cancellation);

        public Task<bool> DeleteHashFieldAsync(string key, string field, CancellationToken cancellation = default) 
            => _controller.GetResponse(new HDeleteCommand(key, field), cancellation);

        public Task<bool> SetHashFieldAsync(string key, string field, byte[] data, CancellationToken cancellation = default)
        {
            if (data == null)
            {
                return DeleteHashFieldAsync(key, field, cancellation);
            }

            var cmd = new HSetCommand(key, field, data);

            return _controller.GetResponse(cmd, cancellation);
        }

        public Task<byte[]> GetHashFieldAsync(string key, string field, CancellationToken cancellation = default)
        {
            return _controller.GetResponse(() => new HGetCommand(key, field), cancellation, ResultConvertion.AsBytes);
        }

        public async Task<IDictionary<string, byte[]>> GetAllHashFieldsAsync(string key, CancellationToken cancellation = default)
        {
            return await _controller.GetResponse(() => new HGetAllCommand(key), cancellation, (x, s) => x.ToDictionary(k => k.Key.ToString(), v => v.Value));
        }

        public async Task<IPersistentDictionary<T>> GetHashSetAsync<T>(string key, CancellationToken cancellation = default)
        {
            return await RedisHashSet<T>.CreateAsync(key, this, _controller.Configuration, GetLock(), cancellation);
        }

        public async Task<long> ScanKeysAsync(ScanOptions scanOptions, CancellationToken cancellation = default)
        {
            var cursor = 0L;
            var resultsCount = 0L;

            while (!cancellation.IsCancellationRequested)
            {
                var cmd = new ScanCommand(scanOptions, cursor);
                var results = await _controller.GetResponse(cmd, cancellation);

                foreach (var key in results.Keys)
                {
                    await scanOptions.ResultsHandler.Invoke(key);

                    resultsCount++;

                    if (scanOptions.MaxCount.HasValue && scanOptions.MaxCount.Value >= resultsCount)
                    {
                        break;
                    }
                }

                if (results.Cursor == 0)
                {
                    break;
                }

                cursor = results.Cursor;
            }

            return resultsCount;
        }

        public Task<IDistributedLock> AquireLockAsync(string key, LockOptions options = default, CancellationToken cancellation = default)
        {
            return _redisLock.AquireLockAsync(KeySpace.Default.GetLockKey(key), options, cancellation);
        }

        public IGraph<T> GetGraph<T>(string graphNamespace)
        {
            if (string.IsNullOrEmpty(graphNamespace))
            {
                throw new ArgumentNullException(nameof(graphNamespace));
            }

            return new Graph<T>(this, _controller.Configuration, new GraphOptions(graphNamespace));
        }

        public void Dispose()
        {
            _controller.Dispose();
        }

        IAsyncLockStrategy<IAsyncLock> GetLock()
        {
            switch (_controller.Configuration.LockStrategy)
            {
                case LockStrategy.Local:
                    return new LocalLockStrategy();
                case LockStrategy.Distributed:
                    return new RedisLock(_controller);
                default:
                    return new NullLock();
            }
        }

        async Task<(T value, bool found)> GetInternalAsync<T>(string key, CancellationToken cancellation = default)
        {
            if (typeof(T) == typeof(byte[]))
            {
                var r = await GetAsync(key, cancellation);

                return ((T)(object)r, r != default);
            }
            if (typeof(T) == typeof(string))
            {
                var r = await GetStringAsync(key, cancellation);

                return ((T)(object)r, r != default);
            }

            return await _controller.GetResponse(() => new ObjectGetCommand<T>(key, _controller.Configuration), cancellation, (x, _) => x);
        }
    }
}