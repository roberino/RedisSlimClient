using RedisTribute.Io.Commands;
using RedisTribute.Types;

namespace RedisTribute.Io.Server
{
    class SelectCommand : RedisCommand<bool>
    {
        readonly int _databaseIndex;

        public SelectCommand(int databaseIndex) : base("SELECT")
        {
            _databaseIndex = databaseIndex;
        }

        protected override CommandParameters GetArgs()
        {
            return new object[] { CommandText, _databaseIndex.ToString() };
        }

        protected override bool TranslateResult(IRedisObject redisObject)
        {
            return string.Equals(redisObject.ToString(), "Ok", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}