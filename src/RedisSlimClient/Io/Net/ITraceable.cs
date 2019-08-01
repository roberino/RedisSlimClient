using System;

namespace RedisSlimClient.Io.Net
{
    interface ITraceable
    {
        event Action<(string Action, byte[] Data)> Trace;
    }
}