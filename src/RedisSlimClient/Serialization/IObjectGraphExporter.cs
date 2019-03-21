namespace RedisSlimClient.Serialization
{
    public interface IObjectGraphExporter<T>
    {
        void WriteObjectData(T instance, IObjectWriter writer);
        //void ReadObjectData(T instance, IObjectReader writer);
    }
}