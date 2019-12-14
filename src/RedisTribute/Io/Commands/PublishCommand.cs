using RedisTribute.Types;
using RedisTribute.Types.Messaging;
using System;

namespace RedisTribute.Io.Commands
{
    class PublishCommand : RedisCommand<int>
    {
        readonly byte[] _message;

        public PublishCommand(IMessage message) : this(message.Body, message.Channel)
        {
        }

        public PublishCommand(byte[] message, RedisKey channel) : base("PUBLISH", true, channel)
        {
            _message = message;
        }

        protected override int TranslateResult(IRedisObject redisObject)
        {
            return (int)redisObject.ToLong();
        }

        protected override CommandParameters GetArgs()
        {
            return new object[] { CommandText, Key.Bytes, _message };
        }
    }
}
