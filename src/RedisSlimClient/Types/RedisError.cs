namespace RedisSlimClient.Types
{
    internal class RedisError : RedisObject
    {
        public RedisError(string message) : base(RedisType.Error)
        {
            Message = message;
        }

        public string Message { get; }
    }
}