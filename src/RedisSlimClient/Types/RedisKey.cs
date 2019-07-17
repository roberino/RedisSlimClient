using System.Text;

namespace RedisSlimClient.Types
{
    readonly struct RedisKey
    {
        private RedisKey(byte[] bytes)
        {
            Bytes = bytes;
        }

        public bool IsNull => Bytes == null;

        public byte[] Bytes { get; }

        public static implicit operator RedisKey(string x) => new RedisKey(Encoding.UTF8.GetBytes(x));

        public static implicit operator RedisKey(byte[] x) => new RedisKey(x);

        public static implicit operator byte[](RedisKey x) => x.Bytes;

        public override string ToString() => Encoding.UTF8.GetString(Bytes);
    }
}