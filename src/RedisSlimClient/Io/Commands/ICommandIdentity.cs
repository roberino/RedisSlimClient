namespace RedisSlimClient.Io.Commands
{
    interface ICommandIdentity
    {
        string CommandText { get; }
        string Key { get; }
    }
}