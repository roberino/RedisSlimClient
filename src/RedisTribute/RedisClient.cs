using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisClient : IRedisClient
    {
        readonly RedisController _controller;

        internal RedisClient(RedisController controller)
        {
            _controller = controller;
        }

        internal static IRedisClient Create(ClientConfiguration configuration, Action onDisposing = null) => 
            new RedisClient(new RedisController(configuration, e => new ConnectionFactory().Create(e), onDisposing));

        public Task<bool> PingAsync(CancellationToken cancellation = default) => _controller.GetResponse(new PingCommand(), cancellation);

        public Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default) 
            => _controller.GetResponses(() => new PingCommand(), 
                (c, r) => new PingResponse(c.AssignedEndpoint, r, ((PingCommand)c).Elapsed), 
                (c, e) => new PingResponse(c.AssignedEndpoint, e, ((PingCommand)c).Elapsed), ConnectionTarget.AllNodes);

        public Task<long> DeleteAsync(string key, CancellationToken cancellation = default) => _controller.GetNumericResponse(new DeleteCommand(key), cancellation);

        public Task<bool> SetAsync(string key, byte[] data, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, data), cancellation);

        public Task<bool> SetAsync(string key, string data, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, _controller.Configuration.Encoding.GetBytes(data)), cancellation);

        public Task<bool> SetAsync<T>(string key, T obj, CancellationToken cancellation = default) => _controller.GetResponse(new ObjectSetCommand<T>(key, _controller.Configuration, obj), cancellation);

        public async Task<Result<T>> GetAsync<T>(string key, CancellationToken cancellation = default)
        {
            try
            {
                var value = await GetInternalAsync<T>(key, cancellation);


                return value == null ? Result<T>.NotFound() : Result<T>.Found(value);
            }
            catch (Io.Commands.KeyNotFoundException)
            {
                return Result<T>.NotFound();
            }
        }

        public Task<byte[]> GetBytesAsync(string key, CancellationToken cancellation = default)
            => _controller.GetResponse(() => new GetCommand(key), cancellation, ResultConvertion.AsBytes);

        public Task<string> GetStringAsync(string key, CancellationToken cancellation = default) 
            => _controller.GetResponse(() => new GetCommand(key), cancellation, ResultConvertion.AsString);

        public async Task<IReadOnlyCollection<string>> GetStringsAsync(IReadOnlyCollection<string> keys, CancellationToken cancellation = default)
        {
            var cmd = new MGetCommand(RedisKeys.FromStrings(keys));

            var results = await _controller.GetMultikeyResultAsync(keys, k => new MGetCommand(k), cancellation);

            return results.SelectMany(s => s.Select(r => r.ToString(_controller.Configuration.Encoding))).ToList();
        }

        public void Dispose()
        {
            _controller.Dispose();
        }

        Task<T> GetInternalAsync<T>(string key, CancellationToken cancellation = default)
        {
            if (typeof(T) == typeof(byte[]))
            {
                return (Task<T>)(object)GetBytesAsync(key, cancellation);
            }
            if (typeof(T) == typeof(string))
            {
                return (Task<T>)(object)GetStringAsync(key, cancellation);
            }

            return _controller.GetResponse(() => new ObjectGetCommand<T>(key, _controller.Configuration), cancellation, (x, _) => x);
        }
    }
}