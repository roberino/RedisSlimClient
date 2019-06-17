namespace RedisSlimClient.Types
{
    internal abstract class RedisObject
    {
        protected RedisObject(RedisType type)
        {
            Type = type;
        }

        public RedisType Type { get; }

        public bool IsNull => Type == RedisType.Null;

        public virtual bool IsComplete => true;
    }
}