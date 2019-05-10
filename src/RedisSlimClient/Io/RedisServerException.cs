using System;

namespace RedisSlimClient.Io
{
    public sealed class RedisServerException : Exception
    {
        public RedisServerException(string msg) : base(msg) { }
    }
}