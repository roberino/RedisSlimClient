using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    class DeleteCommand : RedisPrimativeCommand
    {
        public DeleteCommand(RedisKey key) : base("DEL", true, key)
        {
        }
    }
}