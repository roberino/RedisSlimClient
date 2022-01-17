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

        public Message(string channel, T body, MessageHeader header, ISerializerSettings serializerSettings)
            : this(Guid.NewGuid().ToString("N"), channel, header, new Dictionary<string, string>(), body, serializerSettings)
        {
        }

        Message(string id, string channel, MessageHeader header, IDictionary<string, string> properties, T body, ISerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings;

            Id = id;
            Header = header;
            Body = body;
            Properties = properties;
            Channel = channel;
        }

        public string Id { get; }
        public IDictionary<string, string> Properties { get; }
        public T Body { get; }
        public string Channel { get; }
        public IMessageHeader Header { get; }

        public static IMessage<T> FromBytes(ISerializerSettings serializerSettings, string channel, byte[] data)
        {
            var sm = serializerSettings.Deserialize<SerializableMessage<T>>(data);

            var msg = new Message<T>(sm.Id, channel, sm.Header, sm.Properties, sm.Body, serializerSettings);

            return msg;
        }

        public byte[] GetBytes()
        {
            var sm = new SerializableMessage<T>
            {
                Id = Id,
                Header = (MessageHeader)Header,
                Body = Body,
                Properties = Properties
            };

            return _serializerSettings.SerializeAsBytes(sm);
        }
    }
}