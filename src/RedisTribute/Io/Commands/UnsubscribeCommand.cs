using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class UnsubscribeCommand : RedisCommand<bool>
    {
        readonly HashSet<string> _channels;

        public UnsubscribeCommand(params RedisKey[] channels) : base("UNSUBSCRIBE", channels.Length > 0 ? channels[0] : default)
        {
            if (channels.Length == 0)
            {
                throw new ArgumentException(nameof(channels));
            }

            _channels = new HashSet<string>(channels.Select(c => c.ToString()));
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

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return true;
        }
    }
}
