using System.Threading.Tasks;
using System;

namespace RedisSlimClient.Io.Commands
{
    class SetCommand : RedisCommand
    {
        readonly string _key;
        private readonly byte[] _data;

        public SetCommand(string key, byte[] data) : base("SET")
        {
            _key = key;
            _data = data;
        }

        public override Task WriteAsync(Func<object[], Task> commandWriter)
        {
            return commandWriter(new object[] { CommandText, _key, _data });
        }
    }
}