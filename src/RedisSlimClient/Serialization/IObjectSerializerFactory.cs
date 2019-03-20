namespace RedisSlimClient.Serialization
{
    public interface IObjectSerializerFactory
    {
        IObjectSerializer<T> Create<T>();
    }
}
