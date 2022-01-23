using System;

namespace RedisTribute.Io
{
    public class ConnectionUnavailableException : Exception
    {
        public ConnectionUnavailableException(Uri identifier, Exception? innerException = null) : base(nameof(ConnectionUnavailableException), innerException)
        {
            Identifier = identifier;
        }

        public Uri Identifier { get; }
    }
}