namespace RedisSlimClient.Io.Commands
{
    internal class StringCommand : RedisCommand
    {
        readonly string _value;

        public StringCommand(string commandName, string value) : base(commandName)
        {
            _value = value;
        }
        public override object[] GetArgs() => new object[] { CommandText, _value };
    }
}