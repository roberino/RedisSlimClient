namespace RedisSlimClient.Types
{
    internal class RedisNull : RedisObject
    {
        RedisNull() : base(RedisType.Null) { }

        public static RedisNull Value = new RedisNull();
    }
}
