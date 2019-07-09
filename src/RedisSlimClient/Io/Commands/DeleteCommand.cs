namespace RedisSlimClient.Io.Commands
{
    class DeleteCommand : RedisPrimativeCommand
    {
        public DeleteCommand(string key) : base("DEL", key)
        {
        }
    }
}