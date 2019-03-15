namespace RedisSlimClient.Io.Commands
{
    internal class AuthCommand : StringCommand
    {
        public AuthCommand(string password) : base("AUTH", password)
        {
        }
    }
}