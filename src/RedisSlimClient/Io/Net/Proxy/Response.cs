namespace RedisSlimClient.Io.Net.Proxy
{
    public readonly struct Response
    {
        public Response(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}