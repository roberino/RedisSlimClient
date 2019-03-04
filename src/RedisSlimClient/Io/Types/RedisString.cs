using System.Text;

namespace RedisSlimClient.Io.Types
{
    class RedisString : RedisObject
    {
        public RedisString(byte[] value) : base(RedisType.String)
        {
            Value = value;
        }

        public byte[] Value { get; }

        public string AsString(Encoding encoding = null) => (encoding ?? Encoding.ASCII).GetString(Value);
    }
}