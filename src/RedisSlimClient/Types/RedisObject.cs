namespace RedisSlimClient.Types
{
    class RedisObject
    {
        public RedisObject(RedisType type)
        {
            Type = type;
        }

        public RedisType Type { get; }
    }
}