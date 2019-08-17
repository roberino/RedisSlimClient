namespace RedisSlimClient.Types
{
    interface IRedisObject
    {
        bool IsComplete { get; }
        bool IsNull { get; }
        RedisType Type { get; }
    }
}