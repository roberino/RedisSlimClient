using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    internal abstract class RedisPrimativeCommand : RedisCommand<RedisObject>
    {
        protected RedisPrimativeCommand(string commandText) : base(commandText)
        {
        }

        protected override RedisObject TranslateResult(RedisObject redisObject) => redisObject;
    }
}