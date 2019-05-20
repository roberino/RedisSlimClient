using RedisSlimClient.Serialization.Emit;
using System;

namespace RedisSlimClient.Serialization
{
    public class SerializerFactory : IObjectSerializerFactory
    {
        SerializerFactory()
        {
        }

        public static readonly SerializerFactory Instance = new SerializerFactory();

        public IObjectSerializer<T> Create<T>()
        {
            var tc = Type.GetTypeCode(typeof(T));

            if (tc == TypeCode.Object)
            {
                return TypeProxy<T>.Instance;
            }

            return PrimativeSerializer.CreateSerializer<T>();
        }
    }
}