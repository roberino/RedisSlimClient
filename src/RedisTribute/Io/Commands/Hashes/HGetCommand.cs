using RedisTribute.Types;

namespace RedisTribute.Io.Commands.Hashes
{
    class HGetCommand : RedisPrimativeCommand
    {
        readonly RedisKey _field;

        public HGetCommand(RedisKey key, RedisKey field) : base("HGET", false, key)
        {
            _field = field;
        }

        protected override CommandParameters GetArgs()
        {
            return new object[] { CommandText, Key.Bytes, _field.Bytes };
        }
    }
}