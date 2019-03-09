namespace RedisSlimClient.Serialization
{
    public interface IObjectWriter
    {
        void WriteItem(string name, int level, object data);
    }
}