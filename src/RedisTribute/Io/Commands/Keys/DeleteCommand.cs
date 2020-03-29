using RedisTribute.Types;

namespace RedisTribute.Io.Commands.Keys
{
    class DeleteCommand : RedisPrimativeCommand
    {
        public DeleteCommand(RedisKey key) : base("DEL", true, key)
        {
        }
    }
}