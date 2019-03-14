namespace RedisSlimClient.Types
{
    abstract class RedisObject
    {
        protected RedisObject(RedisType type)
        {
            Type = type;
        }

        public RedisType Type { get; }
    }
}