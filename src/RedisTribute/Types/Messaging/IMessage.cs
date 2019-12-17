using System;
using System.Collections.Generic;

namespace RedisTribute.Types.Messaging
{
    public interface IMessageData
    {
        string Channel { get; }
        byte[] GetBytes();
    }

    public interface IMessage<T> : IMessageData
    {
        string Id { get; }

        DateTime Timestamp { get; }

        MessageFlags Flags { get; }

        IDictionary<string, string> Properties { get; }

        T Body { get; }
    }
}
