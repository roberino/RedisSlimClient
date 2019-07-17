using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    interface ICommandIdentity
    {
        bool RequireMaster { get; }
        string CommandText { get; }
        RedisKey Key { get; }
    }
}