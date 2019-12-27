using System;

namespace RedisTribute.Types.Messaging
{
    [Flags]
    public enum MessageFlags : byte
    {
        None = 0,
        SingleConsumer = 1
    }
}
