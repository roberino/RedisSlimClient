namespace RedisSlimClient.Io.Commands
{
    internal class PingCommand : RedisCommand
    {
        public PingCommand() : base("PING")
        {
        }
    }
}
