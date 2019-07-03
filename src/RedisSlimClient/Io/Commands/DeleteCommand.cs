namespace RedisSlimClient.Io.Commands
{
    class DeleteCommand : StringCommand
    {
        public DeleteCommand(string key) : base("DEL", key)
        {
        }
    }
}