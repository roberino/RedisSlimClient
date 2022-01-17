using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RedisTribute.Types;
using RedisTribute.Types.Messaging;

namespace RedisTribute.Io.Commands.PubSub
{
    class SubscribeCommand : RedisCommand<bool>, ISubscriberCommand
    {
        const string MessageIdentifier = "message";
        const string ConfirmIdentifier = "subscribe";

        readonly TaskCompletionSource<bool> _completionSource;
        readonly Func<IMessageData, Task> _handler;
        readonly HashSet<string> _channels;

        volatile bool _ready;

        public SubscribeCommand(Func<IMessageData, Task> handler, params RedisKey[] channels) : base("SUBSCRIBE", false, channels.Length > 0 ? channels[0] : default)
        {
            if (channels.Length == 0)
            {
                throw new ArgumentException(nameof(channels));
            }

            _completionSource = new TaskCompletionSource<bool>();
            _channels = new HashSet<string>(channels.Select(c => c.ToString()));
            _handler = handler;
        }

        public event Action<Exception>? ConnectionBroken;

        public override void Abandon(Exception ex)
        {
            Stop();

            if (!_ready)
            {
                base.Abandon(ex);
            }
            else
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    return;
                }

                ConnectionBroken?.Invoke(ex);
            }
        }

        public void Stop()
        {
            HasFinished = true;
        }

        public bool HasFinished { get; private set; }

        public bool CanReceive(IRedisObject message)
        {
            if (!_ready)
            {
                return false;
            }

            if (message is RedisArray arr && arr.Count == 3 && string.Equals(arr[0].ToString(), MessageIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                if (arr[1] is RedisString channel)
                {
                    var chan = channel.ToString();

                    if (!_channels.Contains(chan))
                    {
                        return false;
                    }

                    return arr[2] is RedisString;
                }
            }

            return false;
        }

        public Task ReceiveAsync(IRedisObject message)
        {
            if (HasFinished)
            {
                return Task.CompletedTask;
            }

            var arr = (RedisArray)message;
            var chan = (RedisString)arr[1];
            var msg = (RedisString)arr[2];

            var imsg = new Message(msg.Value, chan.Value);

            return _handler.Invoke(imsg);
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            if (redisObject is RedisArray arr && arr.Count == 3 && string.Equals(arr[0].ToString(), ConfirmIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                if (arr[1] is RedisString channel)
                {
                    var chan = channel.ToString();

                    if (_channels.Contains(chan))
                    {
                        _ready = true;

                        return true;
                    }
                }
            }

            return false;
        }

        protected override CommandParameters GetArgs()
        {
            var args = new object[_channels.Count + 1];

            args[0] = CommandText;

            var i = 1;

            foreach (var channel in _channels)
            {
                args[i++] = channel;
            }

            return args;
        }
    }
}
