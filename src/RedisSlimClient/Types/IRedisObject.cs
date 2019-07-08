namespace RedisSlimClient.Types
{
    internal interface IRedisObject
    {
        bool IsComplete { get; }
        bool IsNull { get; }
        RedisType Type { get; }
    }
}