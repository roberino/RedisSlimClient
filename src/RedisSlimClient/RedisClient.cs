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
        readonly ClientConfiguration _configuration;
        readonly ICommandRouter _connection;
        readonly Action _disposing;

        internal RedisClient(ClientConfiguration configuration, Func<ClientConfiguration, ICommandRouter> connectionFactory, Action onDisposing = null)
        {
            _configuration = configuration;
            _connection = connectionFactory(_configuration);
            _disposing = onDisposing ?? (() => { });
        }

        internal static IRedisClient Create(ClientConfiguration configuration, Action onDisposing = null) => new RedisClient(configuration, e => new ConnectionFactory().Create(e), onDisposing);

        public Task<bool> PingAsync(CancellationToken cancellation = default)
        {
            return GetResponse(new PingCommand(), cancellation);
        }

        public Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default)
        {
            return GetResponses(() => new PingCommand(), (x, u) => new PingResponse(u, x), (e, u) => new PingResponse(u, e), ConnectionTarget.AllNodes);
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

            return await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);
        }

        public async Task<T> GetObjectAsync<T>(string key, CancellationToken cancellation = default)
        {
            var cmd = new ObjectGetCommand<T>(key, _configuration);

            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);

            return result;
        }

        public async Task<byte[]> GetBytesAsync(string key, CancellationToken cancellation = default)
        {
            var cmd = new GetCommand(key);

            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);

            return rstr.Value;
        }

        public async Task<string> GetStringAsync(string key, CancellationToken cancellation = default)
        {
            var cmd = new GetCommand(key);

            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);

            return rstr.ToString(_configuration.Encoding);
        }

        public async Task<IReadOnlyCollection<string>> GetStringsAsync(IReadOnlyCollection<string> keys, CancellationToken cancellation = default)
        {
            var cmd = new MGetCommand(RedisKeys.FromStrings(keys));

            var routes = await _connection.RouteMultiKeyCommandAsync(cmd);

            var resultTasks = routes.Select(r =>
                r.Executor.ExecuteWithCancellation(new MGetCommand(r.Keys).AttachTelemetry(_configuration.TelemetryWriter), cancellation,
                    _configuration.DefaultOperationTimeout));

            var results = await Task.WhenAll(resultTasks);

            return results.SelectMany(s => s.Select(r => r.ToString(_configuration.Encoding))).ToList();
        }

        public void Dispose()
        {
            try
            {
                _disposing();
            }
            catch { }

            _connection.Dispose();
        }

        async Task<T> GetResponse<T>(IRedisResult<T> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            return await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);
        }

        async Task<TResult[]> GetResponses<TCmd, TResult>(Func<IRedisResult<TCmd>> cmdFactory, Func<TCmd, Uri, TResult> translator, Func<Exception, Uri, TResult> errorTranslator, ConnectionTarget target, CancellationToken cancellation = default)
        {
            var cmd0 = cmdFactory();

            var cmdPipes = await _connection.RouteCommandAsync(cmd0, target);

            return await Task.WhenAll(cmdPipes.Select(async c =>
            {
                var cmdx = cmdFactory().AttachTelemetry(_configuration.TelemetryWriter);

                try
                {
                    var result = await c.Execute(cmdx, cancellation);

                    return translator(result, cmdx.AssignedEndpoint);
                }
                catch (Exception ex)
                {
                    return errorTranslator(ex, cmdx.AssignedEndpoint);
                }
            }));
        }

        async Task<long> GetIntResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.ExecuteWithCancellation(cmd, cancellation, _configuration.DefaultOperationTimeout);

            var msg = (RedisInteger)result;

            return msg.Value;
        }

        async Task<ICommandExecutor> RouteCommandAsync(IRedisCommand cmd)
        {
            var cmdPipe = await _connection.RouteCommandAsync(cmd);

            cmd.AttachTelemetry(_configuration.TelemetryWriter);

            return cmdPipe;
        }
    }
}