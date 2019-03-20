using RedisSlimClient.Serialization.Emit;

namespace RedisSlimClient.Serialization
{
    public class SerializerFactory : IObjectSerializerFactory
    {
        SerializerFactory()
        {
        }

        public static readonly SerializerFactory Instance = new SerializerFactory();

        public IObjectSerializer<T> Create<T>() => TypeProxy<T>.Instance;
    }
}