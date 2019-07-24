using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public class RedisClient : IRedisClient
    {
        readonly ClientConfiguration _configuration;
        readonly IConnection _connection;

        internal RedisClient(ClientConfiguration configuration) : this(configuration, e => new ConnectionFactory().Create(e))
        {
        }

        internal RedisClient(ClientConfiguration configuration, Func<ClientConfiguration, IConnection> connectionFactory)
        {
            _configuration = configuration;
            _connection = connectionFactory(_configuration);
        }

        public static IRedisClient Create(ClientConfiguration configuration) => new RedisClient(configuration);

        public Task<bool> PingAsync(CancellationToken cancellation = default)
        {
            return GetResponse(new PingCommand(), cancellation);
        }

        public async Task<long> DeleteAsync(string key, CancellationToken cancellation = default)
        {
            return (await GetIntResponse(new DeleteCommand(key), cancellation));
        }

        public Task<bool> SetBytesAsync(string key, byte[] data, CancellationToken cancellation = default)
        {
            return GetResponse(new SetCommand(key, data), cancellation);
        }

        public Task<bool> SetStringAsync(string key, string data, CancellationToken cancellation = default)
        {
            return GetResponse(new SetCommand(key, _configuration.Encoding.GetBytes(data)), cancellation);
        }

        public async Task<bool> SetObjectAsync<T>(string key, T obj, CancellationToken cancellation = default)
        {
            var cmd = new ObjectSetCommand<T>(key, _configuration, obj);

            var cmdPipe = await RouteCommandAsync(cmd);

            return await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));
        }

        public async Task<T> GetObjectAsync<T>(string key, CancellationToken cancellation = default)
        {
            var cmd = new ObjectGetCommand<T>(key, _configuration);

            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));

            return result;
        }

        public async Task<byte[]> GetBytesAsync(string key, CancellationToken cancellation = default)
        {
            var cmd = new GetCommand(key);

            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString) await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));

            return rstr.Value;
        }

        public async Task<string> GetStringAsync(string key, CancellationToken cancellation = default)
        {
            var cmd = new GetCommand(key);

            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));

            return rstr.ToString(_configuration.Encoding);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        async Task<T> GetResponse<T>(IRedisResult<T> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            return await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));
        }

        async Task<bool> CompareStringResponse<T>(IRedisResult<T> cmd, string expectedResponse, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));

            var msg = result.ToString();

            return string.Equals(expectedResponse, msg, StringComparison.OrdinalIgnoreCase);
        }

        async Task<long> GetIntResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.Execute(cmd, CancellationPolicy(cancellation));

            var msg = (RedisInteger)result;

            return msg.Value;
        }

        async Task<ICommandPipeline> RouteCommandAsync(IRedisCommand cmd)
        {
            var cmdPipe = await _connection.RouteCommandAsync(cmd);

            if (_configuration.TelemetryWriter.Enabled)
            {

            }

            return cmdPipe;
        }

        CancellationToken CancellationPolicy(CancellationToken cancellation)
        {
            if (cancellation == default)
            {
                return new CancellationTokenSource(_configuration.DefaultOperationTimeout).Token;
            }

            return cancellation;
        }
    }
}