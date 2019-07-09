using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisPrimativeCommand : RedisCommand<IRedisObject>
    {
        protected RedisPrimativeCommand(string commandText, string key = null) : base(commandText, key)
        {
        }

        protected override IRedisObject TranslateResult(IRedisObject redisObject) => redisObject;
    }
}