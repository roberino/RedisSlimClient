namespace RedisSlimClient.Io.Commands
{
    internal class PingCommand : RedisCommand
    {
        public const string SuccessResponse = "PONG";

        public PingCommand() : base("PING")
        {
        }
    }
}
