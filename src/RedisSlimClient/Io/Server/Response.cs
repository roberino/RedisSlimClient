namespace RedisSlimClient.Io.Server
{
    public class Response
    {
        public Response(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }
}