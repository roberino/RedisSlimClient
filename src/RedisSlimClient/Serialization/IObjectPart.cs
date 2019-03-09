namespace RedisSlimClient.Serialization
{
    public interface IObjectPart
    {
        string Key { get; }
        byte[] Data { get; }
    }
}