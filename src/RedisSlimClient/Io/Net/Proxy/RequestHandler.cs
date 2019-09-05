using System;
using System.Net;
using System.Threading;

namespace RedisSlimClient.Io.Net.Proxy
{
    class RequestHandler
    {
        readonly Func<Request, Response> _handler;

        long _sequence;

        public RequestHandler(Func<Request, Response> handler = null)
        {
            _handler = handler ?? (r => new Response(r.Data));
        }

        public Func<Exception, bool> ErrorHandler { get; set; }

        public int ReceivedBytes { get; private set; }

        public Response Handle(EndPoint remoteEndpoint, byte[] request, int bytesRead)
        {
            ReceivedBytes += bytesRead;
            return _handler(new Request(request, bytesRead, remoteEndpoint, Interlocked.Increment(ref _sequence)));
        }

        public bool HandleError(Exception error)
        {
            return (ErrorHandler?.Invoke(error)).GetValueOrDefault();
        }
    }
}