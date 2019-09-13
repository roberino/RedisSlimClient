using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    abstract class RedisPrimativeCommand : RedisCommand<IRedisObject>
    {
        protected RedisPrimativeCommand(string commandText, bool requireMaster, RedisKey key = default) : base(commandText, requireMaster, key)
        {
        }

        protected override IRedisObject TranslateResult(IRedisObject redisObject) => redisObject;
    }
}