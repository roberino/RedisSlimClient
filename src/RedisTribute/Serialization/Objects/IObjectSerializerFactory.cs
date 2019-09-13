namespace RedisTribute.Serialization
{
    public interface IObjectSerializerFactory
    {
        IObjectSerializer<T> Create<T>();
    }
}
