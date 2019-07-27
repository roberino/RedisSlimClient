using RedisSlimClient.Types;
using System;
using System.Collections.Generic;

namespace RedisSlimClient.Io.Commands
{
    interface IMultiKeyCommandIdentity : ICommandIdentity
    {
        IReadOnlyCollection<RedisKey> Keys { get; }
    }

    interface ICommandIdentity
    {
        Uri AssignedEndpoint { get; set; }
        bool RequireMaster { get; }
        string CommandText { get; }
        RedisKey Key { get; }
    }
}