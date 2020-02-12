using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Commands
{
    class InvalidResponseException : Exception
    {
        public InvalidResponseException(IRedisObject value) : base($"Invalid response from server: { value}") { }
    }
}
