namespace RedisSlimClient.Serialization
{
    public class ObjectPart : IObjectPart
    {
        public string Key { get; set; }

        public byte[] Data { get; set; }
    }
}
