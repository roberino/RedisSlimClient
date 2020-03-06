using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Io.Commands;
using RedisTribute.Io.Server;
using RedisTribute.Types;
using RedisTribute.Types.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Io.Commands.PubSub;

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
                var sw = new Stopwatch();

                sw.Start();

                var msg = Message<T>.FromBytes(_controller.Configuration, m.Channel, m.GetBytes());

                if (msg.Header.Flags.HasFlag(MessageFlags.SingleConsumer))
                {
                    try
                    {
                        var msgLock = await AquireLockAsync(msg.Id, new LockOptions(msg.Header.LockTime, false));

                        var op = WriteTelemetry(msg);

                        try
                        {
                            await handler(msg);
                            WriteTelemetryEnd(msg, sw.Elapsed, op);
                            return;
                        }
                        catch (Exception ex)
                        {
                            await msgLock.ReleaseLockAsync();
                            WriteTelemetryEnd(msg, sw.Elapsed, op, ex);
                            throw;
                        }
                    }
                    catch (SynchronizationLockException)
                    {
                        return;
                    }
                }

                {
                    var op = WriteTelemetry(msg);

                    try
                    {
                        await handler(msg);
                        WriteTelemetryEnd(msg, sw.Elapsed, op);
                    }
                    catch (Exception ex)
                    {
                        WriteTelemetryEnd(msg, sw.Elapsed, op, ex);
                        throw;
                    }
                }

            }, cancellation);
        }

        public async Task<ISubscription> SubscribeAsync(string[] channels, Func<IMessageData, Task> handler, CancellationToken cancellation = default)
        {
            var unsub = new Unsubscriber()
            {
                Unsubscribing = async (targ, rc, ct) =>
                {
                    targ.Stop();

                    lock (_channels)
                    {
                        foreach (var chan in channels)
                        {
                            _channels.Remove(chan);
                        }
                    }

                    if (rc)
                    {
                        var ucmd = new UnsubscribeCommand(channels.Select(c => (RedisKey)c).ToArray());

                        await _controller.GetResponse(ucmd, cancellation);
                    }
                }
            };

            await SubscribeInternalAsync(channels, handler, unsub, cancellation);

            return new Subscription(channels, unsub.Unsubscribe);
        }

        string WriteTelemetry(IMessageData msg)
        {
            if (!_controller.Configuration.TelemetryWriter.Enabled)
            {
                return null;
            }

            var ev = Telemetry.TelemetryEventFactory.Instance.CreateStart(nameof(SubscribeAsync));

            ev.Category = Telemetry.TelemetryCategory.Subscriber;
            ev.Data = msg.Channel;
            ev.Severity = Telemetry.Severity.Info;

            ev.Dimensions[nameof(msg.Channel)] = msg.Channel;

            _controller.Configuration.TelemetryWriter.Write(ev);

            return ev.OperationId;
        }

        void WriteTelemetryEnd(IMessageData msg, TimeSpan elapsed, string opId, Exception ex = null)
        {
            if (!_controller.Configuration.TelemetryWriter.Enabled)
            {
                return;
            }

            var ev = Telemetry.TelemetryEventFactory.Instance.Create(nameof(SubscribeAsync), opId);

            ev.Category = Telemetry.TelemetryCategory.Subscriber;
            ev.Data = msg.Channel;
            ev.Severity = ex == null ? Telemetry.Severity.Info : Telemetry.Severity.Error;
            ev.Sequence = Telemetry.TelemetrySequence.End;

            ev.Dimensions[nameof(msg.Channel)] = msg.Channel;

            _controller.Configuration.TelemetryWriter.Write(ev);
        }

        async Task SubscribeInternalAsync(string[] channels, Func<IMessageData, Task> handler, Unsubscriber unsubscriber, CancellationToken cancellation = default)
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

            unsubscriber.CurrentCommand = cmd;

            cmd.ConnectionBroken += ex =>
            {
                _controller.Configuration.Scheduler.Schedule(async () =>
                {
                    if (unsubscriber.Unsubscribed)
                    {
                        return;
                    }

                    await unsubscriber.UnsubscribeWithoutRemoteCommand();

                    while (!unsubscriber.Unsubscribed)
                    {
                        try
                        {
                            await SubscribeInternalAsync(channels, handler, unsubscriber);
                            return;
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception)
                        {
                            await Task.Delay(1000);
                        }
                    }
                });
            };
        }

        public void Dispose()
        {
            _controller.Dispose();

            if (_lockController.IsValueCreated)
            {
                _lockController.Value.Dispose();
            }
        }

        class Unsubscriber
        {
            long _unFlags;

            public bool Unsubscribed => Interlocked.Read(ref _unFlags) == 1;

            public SubscribeCommand CurrentCommand { get; set; }

            public Func<SubscribeCommand, bool, CancellationToken, Task> Unsubscribing;

            public Task Unsubscribe(CancellationToken cancellation)
            {
                Interlocked.Exchange(ref _unFlags, 1);

                return Unsubscribing?.Invoke(CurrentCommand, true, cancellation);
            }

            public Task UnsubscribeWithoutRemoteCommand() => Unsubscribing?.Invoke(CurrentCommand, false, default);
        }

        Task<IDistributedLock> AquireLockAsync(string key, LockOptions options = default)
        {
            return _redisLock.Value.AquireLockAsync(KeySpace.Default.GetMessageLockKey(key), options);
        }
    }
}
