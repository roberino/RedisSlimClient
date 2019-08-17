namespace RedisSlimClient.Types
{
    readonly struct RedisError : IRedisObject
    {
        public RedisError(string message)
        {
            Message = message;
        }

        public string Message { get; }
        public bool IsComplete => true;
        public bool IsNull => false;
        public RedisType Type => RedisType.Error;

        public override string ToString() => Message;
    }
}