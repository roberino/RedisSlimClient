using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Commands
{
    class StringCommand : RedisCommand
    {
        readonly string _value;

        public StringCommand(string commandName, string value) : base(commandName)
        {
            _value = value;
        }

        public override Task WriteAsync(Func<object[], Task> commandWriter)
        {
            return commandWriter(new object[] { CommandText, _value });
        }
    }
}