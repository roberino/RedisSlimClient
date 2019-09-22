using System.Net;

namespace RedisTribute.Io.Net.Proxy
{
    public readonly struct Request
    {
        public Request(byte[] data, int bytesRead, EndPoint endPoint, long sequence)
        {
            Data = data;
            BytesRead = bytesRead;
            RemoteEndpoint = endPoint;
            Sequence = sequence;
        }

        public EndPoint RemoteEndpoint { get; }
        public long Sequence { get; }
        public byte[] Data { get; }
        public int BytesRead { get; }

        public Response ForwardResponse() => new Response(Data);
    }
}
