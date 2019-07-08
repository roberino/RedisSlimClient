using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisPrimativeCommand : RedisCommand<IRedisObject>
    {
        protected RedisPrimativeCommand(string commandText) : base(commandText)
        {
        }

        protected override IRedisObject TranslateResult(IRedisObject redisObject) => redisObject;
    }
}