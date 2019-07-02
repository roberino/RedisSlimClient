namespace RedisSlimClient.Io.Commands
{
    internal class PingCommand : RedisPrimativeCommand
    {
        public const string SuccessResponse = "PONG";

        public PingCommand() : base("PING")
        {
        }
    }
}
