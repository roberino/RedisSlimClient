using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    internal class GetCommand : RedisPrimativeCommand
    {
        public GetCommand(RedisKey key) : base("GET", false, key)
        {
        }
    }
}