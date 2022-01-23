using System;
using System.Collections.Generic;

namespace RedisTribute.Types.Messaging
{
    public sealed class SerializableMessage<T>
    {
        public SerializableMessage(T body)
        {
            Body = body;
        }

        public string Id { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public MessageFlags Flags { get; init; }
        public MessageHeader Header { get; init; } = MessageHeader.Empty;
        public IDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
        public T Body { get; }
    }
}
