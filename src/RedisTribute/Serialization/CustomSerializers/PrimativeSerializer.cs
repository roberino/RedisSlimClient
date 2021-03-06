﻿using RedisTribute.Serialization.CustomSerializers;
using RedisTribute.Types.Primatives;
using System;
using System.IO;
using System.Text;

namespace RedisTribute.Serialization
{
    static class PrimativeSerializer
    {
        public static IBinaryConverter<T> CreateConverter<T>() => (IBinaryConverter<T>)CreateSerializer<T>();

        public static IObjectSerializer<T> CreateSerializer<T>()
        {
            var tc = Type.GetTypeCode(typeof(T));

            switch (tc)
            {
                case TypeCode.Char:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<char>(BinaryFormatter.Default.ToChar, BinaryFormatter.Default.ToBytes);
                case TypeCode.Boolean:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<bool>(BinaryFormatter.Default.ToBool, BinaryFormatter.Default.ToBytes);
                case TypeCode.Int32:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<int>(BinaryFormatter.Default.ToInt32, BinaryFormatter.Default.ToBytes);
                case TypeCode.Int64:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<long>(BinaryFormatter.Default.ToInt64, BinaryFormatter.Default.ToBytes);
                case TypeCode.Double:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<double>(BinaryFormatter.Default.ToDouble, BinaryFormatter.Default.ToBytes);
                case TypeCode.Decimal:
                    return (IObjectSerializer<T>)new PrimativeSerializerImpl<decimal>(BinaryFormatter.Default.ToDecimal, BinaryFormatter.Default.ToBytes);
                case TypeCode.String:
                    return (IObjectSerializer<T>)new StringSerializer(Encoding.UTF8);
            }

            throw new NotSupportedException(tc.ToString());
        }

        class PrimativeSerializerImpl<T> : IObjectSerializer<T>, IBinaryConverter<T>
        {
            readonly Func<byte[], T> _bytesToItem;
            readonly Func<T, byte[]> _itemToBytes;

            public PrimativeSerializerImpl(Func<byte[], T> bytesToItem, Func<T, byte[]> itemToBytes)
            {
                _bytesToItem = bytesToItem;
                _itemToBytes = itemToBytes;
            }

            public byte[] GetBytes(T value) => _itemToBytes(value);

            public T GetValue(byte[] data) => _bytesToItem(data);

            public T ReadData(IObjectReader reader, T defaulValue) => _bytesToItem(BytesFromStream(reader.Raw()));

            public void WriteData(T instance, IObjectWriter writer) => writer.Raw(_itemToBytes(instance));

            byte[] BytesFromStream(Stream stream)
            {
                if (stream is PooledStream ps)
                {
                    return ps.ToArray();
                }
                if (stream is MemoryStream ms)
                {
                    return ms.ToArray();
                }

                using (var ms2 = new MemoryStream())
                {
                    stream.CopyTo(ms2);
                    return ms2.ToArray();
                }
            }
        }
    }
}