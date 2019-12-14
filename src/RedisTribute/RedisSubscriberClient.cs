using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using RedisTribute.Types.Messaging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute
{
    class RedisSubscriberClient : ISubscriptionClient
    {
        readonly RedisController _controller;
        readonly RedisLock _redisLock;

        internal RedisSubscriberClient(RedisController controller)
        {
            _controller = controller;
            _redisLock = new RedisLock(_controller);
        }

        internal static ISubscriptionClient Create(ClientConfiguration configuration, Action onDisposing = null) =>
            new RedisSubscriberClient(new RedisController(configuration, e => new ConnectionFactory(() => new SubscriberCommandQueue()).Create(e), onDisposing));

        public string ClientName => _controller.Configuration.ClientName;

        public Task<bool> PingAsync(CancellationToken cancellation = default) => _controller.GetResponse(new PingCommand(), cancellation);

        public Task<PingResponse[]> PingAllAsync(CancellationToken cancellation = default)
            => _controller.GetResponses(() => new PingCommand(),
                (c, r, m) => new PingResponse(c.AssignedEndpoint, r, ((PingCommand)c).Elapsed, m),
                (c, e, m) => new PingResponse(c.AssignedEndpoint, e, ((PingCommand)c).Elapsed, m), ConnectionTarget.AllNodes);

        public async Task<ISubscription> SubscribeAsync(string[] channels, Func<IMessage, Task> handler, CancellationToken cancellation = default)
        {
            var cmd = new SubscribeCommand(handler, channels.Select(c => (RedisKey)c).ToArray());

            await _controller.GetResponse(cmd, cancellation);

            return new Subscription(channels, c =>
            {
                cmd.Stop();

                //TODO: Issue unsubscribe cmd

                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            _controller.Dispose();
        }
    }
}
