using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    internal class PingCommand : RedisCommand
    {
        public PingCommand() : base("PING")
        {
        }

        public override Task WriteAsync(Func<object[], Task> commandWriter)
        {
            return commandWriter(new object[] {CommandText});
        }
    }
}
