using System;

namespace RedisTribute.Telemetry
{
    interface ITraceable
    {
        event Action<(string Action, byte[] Data)> Trace;
    }
}