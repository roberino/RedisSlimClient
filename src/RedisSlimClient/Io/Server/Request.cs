namespace RedisSlimClient.Io.Server
{
    class Request
    {
        public Request(byte[] data, int bytesRead)
        {
            Data = data;
            BytesRead = bytesRead;
        }

        public byte[] Data { get; }

        public int BytesRead { get; }
    }
}
