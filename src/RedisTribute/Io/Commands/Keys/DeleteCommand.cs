using RedisTribute.Types;

namespace RedisTribute.Io.Commands
{
    class DeleteCommand : RedisPrimativeCommand
    {
        public DeleteCommand(RedisKey key) : base("DEL", true, key)
        {
        }
    }
}