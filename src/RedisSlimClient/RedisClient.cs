using System;
using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Io.Commands;
using System.Threading.Tasks;

namespace RedisSlimClient
{
    public class RedisClient : IDisposable
    {
        readonly ClientConfiguration _configuration;
        readonly Connection _connection;

        public RedisClient(ClientConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new Connection(_configuration.ServerUri.AsEndpoint());
        }

        public async Task<bool> PingAsync()
        {
            var cmd = new PingCommand();
            var cmdPipe = await _connection.ConnectAsync();

            var result = await cmdPipe.Execute(cmd);

            return true;
        }

        public async Task<byte[]> GetDataAsync(string key)
        {
            var cmd = new GetCommand(key);
            
            var cmdPipe = await _connection.ConnectAsync();

            await cmdPipe.Execute(cmd);

            await cmd.Result;

            return (byte[])cmd.Result.Result;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
