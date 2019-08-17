namespace RedisSlimClient.Io.Server
{
    class Response
    {
        public Response(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}