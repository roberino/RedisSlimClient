using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    class GetCommand : RedisPrimativeCommand
    {
        public GetCommand(RedisKey key) : base("GET", false, key)
        {
        }
    }
}