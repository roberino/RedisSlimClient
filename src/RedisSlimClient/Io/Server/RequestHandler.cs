using System;

namespace RedisSlimClient.Io.Server
{
    class RequestHandler
    {
        readonly Func<Request, Response> _handler;

        public RequestHandler(Func<Request, Response> handler = null)
        {
            _handler = handler ?? (r => new Response(r.Data));
        }

        public Func<Exception, bool> ErrorHandler { get; set; }

        public int ReceivedBytes { get; private set; }

        public Response Handle(byte[] request, int bytesRead)
        {
            ReceivedBytes += bytesRead;
            return _handler(new Request(request, bytesRead));
        }

        public bool HandleError(Exception error)
        {
            return (ErrorHandler?.Invoke(error)).GetValueOrDefault();
        }
    }
}