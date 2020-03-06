using RedisTribute.Types;
using RedisTribute.Types.Messaging;

namespace RedisTribute.Io.Commands.PubSub
{
    class PublishCommand : RedisCommand<int>
    {
        readonly byte[] _message;

        public PublishCommand(IMessageData message) : this(message.GetBytes(), message.Channel)
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
