namespace RedisSlimClient.Serialization
{
    public interface IObjectGraphExporter
    {
        void WriteObjectData(object instance, IObjectWriter writer);
    }
}