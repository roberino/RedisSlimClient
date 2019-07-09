namespace RedisSlimClient.Io.Commands
{
    internal class AuthCommand : RedisPrimativeCommand
    {
        private readonly string _password;

        public AuthCommand(string password) : base("AUTH")
        {
            _password = password;
        }

        public override object[] GetArgs() => new object[] { CommandText, _password };
    }
}