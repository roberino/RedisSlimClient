namespace RedisSlimClient.Io.Commands
{
    class GetCommand : StringCommand
    {
        public GetCommand(string key) : base("GET", key)
        {
        }
    }
}
