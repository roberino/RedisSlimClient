using RedisSlimClient.Types;
using System;

namespace RedisSlimClient.Io.Commands
{
    interface ICommandIdentity
    {
        Uri AssignedEndpoint { get; set; }
        bool RequireMaster { get; }
        string CommandText { get; }
        RedisKey Key { get; }
    }
}