using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class HSetCommand : RedisCommand<bool>
    {
        public const string SuccessResponse = "OK";
        readonly RedisKey _field;
        readonly byte[] _data;

        public HSetCommand(RedisKey key, RedisKey field, byte[] data): base("HSET", key)
        {
            _field = field;
            _data = data;
        }

        protected override CommandParameters GetArgs()
        {
            return new object[] { CommandText, Key.Bytes, _field.Bytes, _data };
        }

        protected override bool TranslateResult(IRedisObject redisObject) => (redisObject is RedisInteger i && i > 0);
    }
}
