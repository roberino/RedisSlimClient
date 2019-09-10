using System;

namespace RedisSlimClient.Io.Server
{
    public sealed class PingResponse
    {
        internal PingResponse(Uri serverEndpoint, bool ok, TimeSpan elapsed)
        {
            Endpoint = serverEndpoint;
            Ok = ok;
            Elapsed = elapsed;
        }

        internal PingResponse(Uri serverEndpoint, Exception ex, TimeSpan elapsed)
        {
            Endpoint = serverEndpoint;
            Ok = false;
            Error = ex;
            Elapsed = elapsed;
        }

        public Uri Endpoint { get; }
        public Exception Error { get; }
        public TimeSpan Elapsed { get; }
        public bool Ok { get; }
    }
}
