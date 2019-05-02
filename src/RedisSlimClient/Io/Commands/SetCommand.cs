namespace RedisSlimClient.Io.Commands
{
    internal class SetCommand : RedisCommand
    {
        readonly string _key;
        readonly byte[] _data;

        public SetCommand(string key, byte[] data) : base("SET")
        {
            _key = key;
            _data = data;
        }

        public override object[] GetArgs() => new object[] { CommandText, _key, _data };
    }
}