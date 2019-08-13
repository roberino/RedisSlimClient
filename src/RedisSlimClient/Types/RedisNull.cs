namespace RedisSlimClient.Types
{
    readonly struct RedisNull : IRedisObject
    {
        public static RedisNull Value = new RedisNull();

        public bool IsComplete => true;
        public bool IsNull => true;
        public RedisType Type => RedisType.Null;

        public override string ToString() => Type.ToString();
    }
}