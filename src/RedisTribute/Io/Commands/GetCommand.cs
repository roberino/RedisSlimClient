using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class GetCommand : RedisPrimativeCommand
    {
        public GetCommand(RedisKey key) : base("GET", false, key)
        {
        }
    }
}