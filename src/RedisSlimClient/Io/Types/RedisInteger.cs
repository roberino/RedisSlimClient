namespace RedisSlimClient.Io.Types
{
    class RedisInteger : RedisObject
    {
        public RedisInteger(long value) : base(RedisType.Integer)
        {
            Value = value;
        }

        public long Value { get; }
    }
}
