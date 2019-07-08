namespace RedisSlimClient.Types
{
    internal readonly struct RedisNull : IRedisObject
    {
        public static RedisNull Value = new RedisNull();

        public bool IsComplete => true;
        public bool IsNull => true;
        public RedisType Type => RedisType.Null;
    }
}