using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient
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

        public Task<bool> SetBytesAsync(string key, byte[] data, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, data), cancellation);

        public Task<bool> SetStringAsync(string key, string data, CancellationToken cancellation = default) => _controller.GetResponse(new SetCommand(key, _controller.Configuration.Encoding.GetBytes(data)), cancellation);

        public Task<bool> SetObjectAsync<T>(string key, T obj, CancellationToken cancellation = default) => _controller.GetResponse(new ObjectSetCommand<T>(key, _controller.Configuration, obj), cancellation);

        public Task<T> GetObjectAsync<T>(string key, CancellationToken cancellation = default) => _controller.GetResponse<T>(new ObjectGetCommand<T>(key, _controller.Configuration), cancellation);

        public Task<byte[]> GetBytesAsync(string key, CancellationToken cancellation = default) => _controller.GetBinaryResponse(new GetCommand(key));

        public Task<string> GetStringAsync(string key, CancellationToken cancellation = default) => _controller.GetTextResponse(new GetCommand(key), cancellation);

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
    }
}