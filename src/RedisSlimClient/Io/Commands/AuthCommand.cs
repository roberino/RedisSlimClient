namespace RedisSlimClient.Io.Commands
{
    class AuthCommand : StringCommand
    {
        public AuthCommand(string password) : base("AUTH", password)
        {
        }
    }
}