namespace RedisSlimClient.Types
{
    internal readonly struct RedisInteger : IRedisObject
    {
        public RedisInteger(long value)
        {
            Value = value;
        }

        public long Value { get; }
        public bool IsComplete => true;
        public bool IsNull => false;
        public RedisType Type => RedisType.Integer;

        public static implicit operator long(RedisInteger x) => x.Value;

        public override string ToString() => Value.ToString();
    }
}
