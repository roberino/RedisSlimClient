using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class TimeCommand : RedisCommand<UnixTime>
    {
        public TimeCommand() : base("TIME")
        {
        }

        protected override UnixTime TranslateResult(IRedisObject redisObject)
        {
            return new UnixTime(((RedisArray) redisObject).ToLong());
        }
    }
}
