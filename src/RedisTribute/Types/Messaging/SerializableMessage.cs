using System;
using System.Collections.Generic;

namespace RedisTribute.Types.Messaging
{
    public sealed class SerializableMessage<T>
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageFlags Flags { get; set; }
        public MessageHeader Header { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public T Body { get; set; }
    }
}
