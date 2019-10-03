using RedisTribute.Io.Commands;
using RedisTribute.Types;

namespace RedisTribute.Io.Server.Transactions
{
    class DiscardTransactionCommand : RedisCommand<bool>
    {
        public DiscardTransactionCommand() : base("DISCARD")
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
