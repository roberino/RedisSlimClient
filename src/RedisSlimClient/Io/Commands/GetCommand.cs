namespace RedisSlimClient.Io.Commands
{
    internal class GetCommand : StringCommand
    {
        public GetCommand(string key) : base("GET", key)
        {
        }
    }
}
