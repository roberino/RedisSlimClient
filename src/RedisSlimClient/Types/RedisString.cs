using System.IO;
using System.Text;

namespace RedisSlimClient.Types
{
    internal class RedisString : RedisObject
    {
        public RedisString(byte[] value) : base(RedisType.String)
        {
            Value = value;
        }

        public byte[] Value { get; }

        public string ToString(Encoding encoding) => encoding.GetString(Value);

        public override string ToString() => ToString(Encoding.ASCII);

        public Stream ToStream() => new MemoryStream(Value);
    }
}