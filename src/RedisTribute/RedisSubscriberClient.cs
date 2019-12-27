using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using RedisTribute.Types.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisSubscriberClient : ISubscriptionClient
    {
        readonly RedisController _controller;
        readonly Lazy<RedisController> _lockController;
        readonly Lazy<RedisLock> _redisLock;
        readonly HashSet<string> _channels;

        RedisSubscriberClient(RedisController controller, Func<RedisController> lockControllerFactory)
        {
            _controller = controller;
            _lockController = new Lazy<RedisController>(lockControllerFactory, LazyThreadSafetyMode.ExecutionAndPublication);
            _redisLock = new Lazy<RedisLock>(() => new RedisLock(_lockController.Value), LazyThreadSafetyMode.ExecutionAndPublication);
            _channels = new HashSet<string>();
        }

        internal static ISubscriptionClient Create(ClientConfiguration configuration, Action onDisposing = null) =>
            new RedisSubscriberClient(new RedisController(configuration, 
                e => new ConnectionFactory(() => new SubscriberCommandQueue(), PipelineMode.AsyncPipeline).Create(e), onDisposing), 
                () => new RedisController(configuration, c => new ConnectionFactory().Create(c)));

        public string ClientName => _controller.Configuration.ClientName;

        public Task<bool> PingAsync(CancellationToken cancellation = default) => _controller.GetResponse(new PingCommand(), cancellation);

        public Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default)
            => _controller.GetResponses(() => new PingCommand(),
                (c, r, m) => new PingResponse(c.AssignedEndpoint, r, ((PingCommand)c).Elapsed, m),
                (c, e, m) => new PingResponse(c.AssignedEndpoint, e, ((PingCommand)c).Elapsed, m), ConnectionTarget.AllNodes);

        public async Task<ISubscription> SubscribeAsync<T>(string[] channels, Func<IMessage<T>, Task> handler, CancellationToken cancellation = default)
        {
            return await SubscribeAsync(channels, async m =>
            {
                var msg = Message<T>.FromBytes(_controller.Configuration, m.Channel, m.GetBytes());

                if (msg.Header.Flags.HasFlag(MessageFlags.SingleConsumer))
                {
                    try
                    {
                        using (var msgLock = await AquireLockAsync(msg.Id, new LockOptions(msg.Header.LockTime, false)))
                        {
                            try
                            {
                                await handler(msg);
                                return;
                            }
                            catch
                            {
                                await msgLock.ReleaseLockAsync();
                                throw;
                            }
                        }
                    }
                    catch (SynchronizationLockException)
                    {
                        return;
                    }
                }

                await handler(msg);
            }, cancellation);
        }

        public async Task<ISubscription> SubscribeAsync(string[] channels, Func<IMessageData, Task> handler, CancellationToken cancellation = default)
        {
            lock (_channels)
            {
                foreach (var chan in channels)
                {
                    if (_channels.Contains(chan))
                    {
                        throw new NotSupportedException($"Already a subscriber to {chan}");
                    }
                }
                foreach (var chan in channels)
                {
                    _channels.Add(chan);
                }
            }

            var cmd = new SubscribeCommand(handler, channels.Select(c => (RedisKey)c).ToArray());

            await _controller.GetResponse(cmd, cancellation);

            return new Subscription(channels, async ct =>
            {
                cmd.Stop();

                lock (_channels)
                {
                    foreach (var chan in channels)
                    {
                        _channels.Remove(chan);
                    }
                }

                var ucmd = new UnsubscribeCommand(channels.Select(c => (RedisKey)c).ToArray());

                await _controller.GetResponse(ucmd, cancellation);
            });
        }

        public void Dispose()
        {
            _controller.Dispose();

            if (_lockController.IsValueCreated)
            {
                _lockController.Value.Dispose();
            }
        }

        Task<IDistributedLock> AquireLockAsync(string key, LockOptions options = default)
        {
            return _redisLock.Value.AquireLockAsync($"$$_msglock:{key}", options);
        }
    }
}
