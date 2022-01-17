using System;
using System.Collections.Generic;

namespace RedisTribute.Types.Messaging
{
    public sealed class SerializableMessage<T>
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public MessageFlags Flags { get; set; }
        public MessageHeader Header { get; set; }
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public T Body { get; set; }
    }
}
