using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisPrimativeCommand : RedisCommand<IRedisObject>
    {
        protected RedisPrimativeCommand(string commandText, bool requireMaster, RedisKey key = default) : base(commandText, requireMaster, key)
        {
        }

        protected override IRedisObject TranslateResult(IRedisObject redisObject) => redisObject;
    }
}