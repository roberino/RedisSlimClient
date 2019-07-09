namespace RedisSlimClient.Io.Commands
{
    internal class GetCommand : RedisPrimativeCommand
    {
        public GetCommand(string key) : base("GET", key)
        {
        }
    }
}
