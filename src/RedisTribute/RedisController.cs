﻿using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisController : IDisposable
    {
        readonly ICommandRouter _connection;
        readonly Action _disposing;

        public RedisController(ClientConfiguration configuration, Func<ClientConfiguration, ICommandRouter> connectionFactory, Action onDisposing = null)
        {
            Configuration = configuration;
            _connection = connectionFactory(Configuration);
            _disposing = onDisposing ?? (() => { });
        }

        public ClientConfiguration Configuration { get; }

        public async Task<T> GetResponse<T>(IRedisResult<T> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            return await cmdPipe.ExecuteWithCancellation(cmd, cancellation, Configuration.DefaultOperationTimeout);
        }

        public async Task<TResult> GetResponse<T, TResult>(Func<IRedisResult<T>> cmdFactory, CancellationToken cancellation, Func<T, ISerializerSettings, TResult> resultConverter)
        {
            if (Configuration.FallbackStrategy == FallbackStrategy.None)
            {
                return resultConverter(await GetResponse(cmdFactory(), cancellation), Configuration);
            }

            if (cancellation == default)
            {
                using (var cancellationSource = new CancellationTokenSource(Configuration.DefaultOperationTimeout))
                {
                    if (Configuration.FallbackStrategy == FallbackStrategy.ProactiveRetry)
                    {
                        return resultConverter(await GetResponseWithRetryProactive(cmdFactory, cancellationSource.Token), Configuration);
                    }

                    return resultConverter(await GetResponseWithRetry(cmdFactory, cancellationSource.Token), Configuration);
                }
            }

            if (Configuration.FallbackStrategy == FallbackStrategy.ProactiveRetry)
            {
                return resultConverter(await GetResponseWithRetryProactive(cmdFactory, cancellation), Configuration);
            }

            return resultConverter(await GetResponseWithRetry(cmdFactory, cancellation), Configuration);
        }

        public async Task<TResult[]> GetResponses<TCmd, TResult>(Func<IRedisResult<TCmd>> cmdFactory, Func<IRedisResult<TCmd>, TCmd, TResult> translator, Func<IRedisResult<TCmd>, Exception, TResult> errorTranslator, ConnectionTarget target, CancellationToken cancellation = default)
        {
            var cmd0 = cmdFactory();

            var cmdPipes = await _connection.RouteCommandAsync(cmd0, target);

            return await Task.WhenAll(cmdPipes.Select(async c =>
            {
                var cmdx = cmdFactory().AttachTelemetry(Configuration.TelemetryWriter);

                try
                {
                    var result = await c.Execute(cmdx, cancellation);

                    return translator(cmdx, result);
                }
                catch (Exception ex)
                {
                    return errorTranslator(cmdx, ex);
                }
            }));
        }

        public async Task<long> GetNumericResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var result = await cmdPipe.ExecuteWithCancellation(cmd, cancellation, Configuration.DefaultOperationTimeout);

            var msg = (RedisInteger)result;

            return msg.Value;
        }

        public async Task<string> GetTextResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.ExecuteWithCancellation(cmd, cancellation, Configuration.DefaultOperationTimeout);

            return rstr.ToString(Configuration.Encoding);
        }

        public async Task<byte[]> GetBinaryResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.ExecuteWithCancellation(cmd, cancellation, Configuration.DefaultOperationTimeout);

            return rstr.Value;
        }

        public async Task<byte[]> GetFirstAvailableResponse(IRedisResult<IRedisObject> cmd, CancellationToken cancellation = default)
        {
            var cmdPipe = await RouteCommandAsync(cmd);

            var rstr = (RedisString)await cmdPipe.ExecuteWithCancellation(cmd, cancellation, Configuration.DefaultOperationTimeout);

            return rstr.Value;
        }

        public async Task<IReadOnlyCollection<TCmd>> GetMultikeyResultAsync<TCmd>(IReadOnlyCollection<string> keys, Func<RedisKey[], IRedisResult<TCmd>> cmdFactory,  CancellationToken cancellation = default)
        {
            var cmd = new MGetCommand(RedisKeys.FromStrings(keys));

            var routes = await RouteMultiKeyCommandAsync(cmd);

            var resultTasks = routes.Select(r =>
                r.Executor.ExecuteWithCancellation(cmdFactory(r.Keys).AttachTelemetry(Configuration.TelemetryWriter), cancellation,
                    Configuration.DefaultOperationTimeout));

            var results = await Task.WhenAll(resultTasks);

            return results;
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

        async Task<T> GetResponseWithRetry<T>(Func<IRedisResult<T>> cmdFactory, CancellationToken cancellation)
        {
            var attempt = 1;
            IRedisResult<T> currentResult = null;

            while (!cancellation.IsCancellationRequested)
            {
                currentResult = cmdFactory();

                currentResult.AttemptSequence = attempt++;

                try
                {
                    var cmdPipe = await RouteCommandAsync(currentResult);

                    return await cmdPipe.ExecuteWithCancellation(currentResult, cancellation, Configuration.DefaultOperationTimeout);
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        throw;
                    }
                }
            }

            throw new TaskCanceledException();
        }

        async Task<T> GetResponseWithRetryProactive<T>(Func<IRedisResult<T>> cmdFactory, CancellationToken cancellation)
        {
            var attempt = 1;

            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    ICommandExecutor currentPipe = null;

                    async Task<(T value, bool success)> GetResult()
                    {
                        var currentResult = cmdFactory();

                        currentResult.AttemptSequence = attempt++;

                        var nextPipe = await RouteCommandAsync(currentResult);

                        if (ReferenceEquals(nextPipe, currentPipe))
                        {
                            return (default, false);
                        }

                        currentPipe = nextPipe;

                        return (await currentPipe.ExecuteWithCancellation(currentResult, cancellation, Configuration.DefaultOperationTimeout), true);
                    }

                    var resultTask = GetResult();

                    await Task.WhenAny(resultTask, Task.Delay(Configuration.OptimisticOperationTimeout, cancellation));

                    if (!resultTask.IsCompleted)
                    {
                        var resultTaskFallback = GetResult();

                        await Task.WhenAny(resultTask, resultTaskFallback);
    
                        if (resultTaskFallback.IsCompleted && resultTaskFallback.Result.success)
                        {
                            return resultTaskFallback.Result.value;
                        }

                        return (await resultTask).value;
                    }

                    if (resultTask.IsCompleted && resultTask.Result.success)
                    {
                        return resultTask.Result.value;
                    }
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        throw;
                    }
                }
            }

            throw new TaskCanceledException();
        }

        async Task<ICommandExecutor> RouteCommandAsync(IRedisCommand cmd)
        {
            var cmdPipe = await _connection.RouteCommandAsync(cmd);

            _ = cmd.AttachTelemetry(Configuration.TelemetryWriter);

            return cmdPipe;
        }

        Task<IReadOnlyCollection<MultiKeyRoute>> RouteMultiKeyCommandAsync(IMultiKeyCommandIdentity multiKeyCommand)
        {
            return _connection.RouteMultiKeyCommandAsync(multiKeyCommand);
        }
    }
}