using RedisTribute.Configuration;
using RedisTribute.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisTribute.Types.Messaging
{
    readonly struct Message : IMessageData
    {
        public Message(byte[] body, string channel)
        {
            Body = body;
            Channel = channel;
        }

        public Message(byte[] body, byte[] channel)
        {
            Body = body;
            Channel = Encoding.UTF8.GetString(channel);
        }

        public Message(string body, string channel)
        {
            Body = Encoding.UTF8.GetBytes(body);
            Channel = channel;
        }

        public byte[] Body { get; }
        public string Channel { get; }

        public byte[] GetBytes()
        {
            return Body;
        }

        public override string ToString()
        {
            return Body == null ? null : Encoding.UTF8.GetString(Body);
        }
    }

    class Message<T> : IMessage<T>
    {
        private readonly ISerializerSettings _serializerSettings;

        public Message(T body, string channel, ISerializerSettings serializerSettings, MessageFlags flags = MessageFlags.None)
        {
            _serializerSettings = serializerSettings;

            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            Properties = new Dictionary<string, string>();
            Channel = channel;
            Body = body;
        }

        Message(string id, DateTime timestamp, IDictionary<string, string> properties, T body, string channel, MessageFlags flags, ISerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings;

            Id = id;
            Timestamp = timestamp;
            Body = body;
            Properties = properties;
            Channel = channel;
            Flags = flags;
        }

        public string Id { get; }
        public IDictionary<string, string> Properties { get; }
        public T Body { get; }
        public string Channel { get; }
        public DateTime Timestamp { get; }
        public MessageFlags Flags { get; }

        public static IMessage<T> FromBytes(ISerializerSettings serializerSettings, string channel, byte[] data)
        {
            var sm = serializerSettings.Deserialize<SerializableMessage<T>>(data);

            var msg = new Message<T>(sm.Id, sm.Timestamp, sm.Properties, sm.Body, channel, sm.Flags, serializerSettings);

            return msg;
        }

        public byte[] GetBytes()
        {
            var sm = new SerializableMessage<T>
            {
                 Id = Id,
                 Timestamp = Timestamp,
                 Body = Body,
                 Properties = Properties
            };

            return _serializerSettings.SerializeAsBytes(sm);
        }
    }
}