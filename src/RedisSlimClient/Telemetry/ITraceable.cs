using System;

namespace RedisSlimClient.Telemetry
{
    interface ITraceable
    {
        event Action<(string Action, byte[] Data)> Trace;
    }
}