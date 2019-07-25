using System;

namespace RedisSlimClient.Io.Server
{
    public sealed class PingResponse
    {
        internal PingResponse(Uri serverEndpoint, bool ok)
        {
            Endpoint = serverEndpoint;
            Ok = ok;
        }

        internal PingResponse(Uri serverEndpoint, Exception ex)
        {
            Endpoint = serverEndpoint;
            Ok = false;
            Error = ex;
        }

        public Uri Endpoint { get; }
        public Exception Error { get; }
        public bool Ok { get; }
    }
}
