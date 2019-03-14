using System.IO;
using System.Text;
using RedisSlimClient.Io;

namespace RedisSlimClient.Types
{
    class RedisString : RedisObject
    {
        public RedisString(byte[] value) : base(RedisType.String)
        {
            Value = value;
        }

        public byte[] Value { get; }

        public string ToString(Encoding encoding) => encoding.GetString(Value);

        public override string ToString() => ToString(Encoding.ASCII);
    }
}