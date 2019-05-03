using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public class RedisClient : IDisposable
    {
        readonly ClientConfiguration _configuration;
        readonly IConnection _connection;

        public RedisClient(ClientConfiguration configuration) : this(configuration, e => new Connection(e))
        {
        }

        internal RedisClient(ClientConfiguration configuration, Func<EndPoint, IConnection> connectionFactory)
        {
            _configuration = configuration;
            _connection = connectionFactory(_configuration.ServerUri.AsEndpoint());
        }

        public async Task<bool> PingAsync()
        {
            return await CompareStringResponse(new PingCommand(), "PONG");
        }

        public async Task<bool> SetDataAsync(string key, byte[] data)
        {
            return await CompareStringResponse(new SetCommand(key, data), "OK");
        }

        public async Task<bool> SetObjectAsync<T>(string key, T obj)
        {
            var cmd = new ObjectSetCommand<T>(key, _configuration, obj);

            var cmdPipe = await _connection.ConnectAsync();

            return await cmdPipe.Execute(cmd, _configuration.DefaultTimeout);
        }

        public async Task<T> GetObjectAsync<T>(string key)
        {
            var cmd = new ObjectGetCommand<T>(key, _configuration);

            var cmdPipe = await _connection.ConnectAsync();

            var result = await cmdPipe.Execute(cmd, _configuration.DefaultTimeout);

            return result;
        }

        public async Task<byte[]> GetDataAsync(string key)
        {
            var cmd = new GetCommand(key);

            var cmdPipe = await _connection.ConnectAsync();

            var rstr = (RedisString) await cmdPipe.Execute(cmd, _configuration.DefaultTimeout);

            return rstr.Value;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        async Task<bool> CompareStringResponse<T>(IRedisResult<T> cmd, string expectedResponse)
        {
            var cmdPipe = await _connection.ConnectAsync();

            var result = await cmdPipe.Execute(cmd, _configuration.DefaultTimeout);

            var msg = result.ToString();

            return string.Equals(expectedResponse, msg, StringComparison.OrdinalIgnoreCase);
        }
    }
}
