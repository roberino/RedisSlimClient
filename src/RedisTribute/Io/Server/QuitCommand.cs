using RedisTribute.Io.Commands;
using RedisTribute.Types;

namespace RedisTribute.Io.Server
{
    class QuitCommand : RedisCommand<bool>
    {
        public QuitCommand() : base("QUIT")
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}