using System;

namespace RedisTribute.Io
{
    public sealed class RedisServerException : Exception
    {
        public RedisServerException(string msg) : base(msg) { }
    }
}