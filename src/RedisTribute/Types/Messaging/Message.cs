using System.Text;

namespace RedisTribute.Types.Messaging
{
    public readonly struct Message : IMessage
    {
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

        public override string ToString()
        {
            return Body == null ? null : Encoding.UTF8.GetString(Body);
        }
    }
}