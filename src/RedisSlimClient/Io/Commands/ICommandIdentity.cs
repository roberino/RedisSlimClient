using RedisSlimClient.Io.Server;
using RedisSlimClient.Types;

namespace RedisSlimClient.Io.Commands
{
    interface ICommandIdentity
    {
        IRedisEndpoint AssignedEndpoint { get; set; }
        bool RequireMaster { get; }
        string CommandText { get; }
        RedisKey Key { get; }
    }
}