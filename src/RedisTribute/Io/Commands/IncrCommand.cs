using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class IncrCommand : RedisCommand<long>
    {
        public IncrCommand(RedisKey key) : base("INCR", true, key)
        {
        }

        protected override long TranslateResult(IRedisObject redisObject)
        {
            return redisObject.ToLong();
        }
    }
}