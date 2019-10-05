using RedisTribute.Io.Commands;
using RedisTribute.Types;

namespace RedisTribute.Io.Server.Transactions
{
    class BeginTransactionCommand : RedisCommand<bool>
    {
        public BeginTransactionCommand() : base("MULTI")
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}