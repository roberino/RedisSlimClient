using RedisTribute.Io.Commands;
using RedisTribute.Types;

namespace RedisTribute.Io.Server.Transactions
{
    class ExecuteTransactionCommand : RedisCommand<bool>
    {
        public ExecuteTransactionCommand() : base("EXEC")
        {
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
