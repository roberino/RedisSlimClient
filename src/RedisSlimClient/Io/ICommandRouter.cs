using RedisSlimClient.Io.Commands;

namespace RedisSlimClient.Io
{
    interface ICommandRouter
    {
        IConnection Route(ICommandIdentity command);
    }
}