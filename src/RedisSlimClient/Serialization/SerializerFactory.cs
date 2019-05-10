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

            switch (tc)
            {
                case TypeCode.Char:
                    return (IObjectSerializer<T>)new PrimativeSerializer<char>(BinaryFormatter.Default.ToChar, BinaryFormatter.Default.ToBytes);
                case TypeCode.Boolean:
                    return (IObjectSerializer<T>)new PrimativeSerializer<bool>(BinaryFormatter.Default.ToBool, BinaryFormatter.Default.ToBytes);
                case TypeCode.Int32:
                    return (IObjectSerializer<T>)new PrimativeSerializer<int>(BinaryFormatter.Default.ToInt32, BinaryFormatter.Default.ToBytes);
                case TypeCode.Int64:
                    return (IObjectSerializer<T>)new PrimativeSerializer<long>(BinaryFormatter.Default.ToInt64, BinaryFormatter.Default.ToBytes);
                case TypeCode.Double:
                    return (IObjectSerializer<T>)new PrimativeSerializer<double>(BinaryFormatter.Default.ToDouble, BinaryFormatter.Default.ToBytes);
                case TypeCode.Decimal:
                    return (IObjectSerializer<T>)new PrimativeSerializer<decimal>(BinaryFormatter.Default.ToDecimal, BinaryFormatter.Default.ToBytes);
            }

            return TypeProxy<T>.Instance;
        }
    }
}