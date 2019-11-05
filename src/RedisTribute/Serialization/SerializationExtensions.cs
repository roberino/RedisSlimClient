using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Types;
using RedisTribute.Types.Primatives;
using System;
using System.Collections;
using System.IO;

namespace RedisTribute.Serialization
{
    static class SerializationExtensions
    {
        public static bool AreBinaryEqual<T>(this ISerializerSettings serializerSettings, byte[] serializedData, T preSerializedValue)
        {
            var originalLocal = serializerSettings.SerializeAsBytes(preSerializedValue);
            return StructuralComparisons.StructuralEqualityComparer.Equals(serializedData, originalLocal);
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, IRedisObject result)
        {
            return Deserialize(serializerSettings, serializerSettings.SerializerFactory.Create<T>(), result);
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, IObjectSerializer<T> serializer, IRedisObject result)
        {
            if (result is RedisString strData)
            {
                using (strData)
                {
                    return serializerSettings.Deserialize<T>(serializer, strData.AsStream());
                }
            }

            throw new ArgumentException($"{result.Type}");
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, IObjectSerializer<T> serializer, Stream data)
        {
            var byteSeq = new ArraySegmentToRedisObjectReader(new StreamIterator(data));
            var objReader = new ObjectReader(byteSeq, data, serializerSettings.Encoding, null, serializerSettings.SerializerFactory);

            return serializer.ReadData(objReader, default);
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, IObjectSerializer<T> serializer, byte[] data)
        {
            using(var ms = StreamPool.Instance.CreateReadonly(data))
            {
                return serializerSettings.Deserialize(serializer, ms);
            }
        }

        public static PooledStream Serialize<T>(this ISerializerSettings serializerSettings, T data)
        {
            var serializer = new ObjectDeserializer<T>(serializerSettings);

            return serializer.GetObjectData(data);
        }

        public static byte[] SerializeAsBytes<T>(this ISerializerSettings serializerSettings, T data)
        {
            using (var stream = serializerSettings.Serialize(data))
            {
                return stream.ToArray();
            }
        }

        class ObjectDeserializer<T>
        {
            const int MaxBufferSize = 128 * 1024 * 1000;

            static readonly object _lockObj = new object();
            static int _currentMaxBufferSize = 1024 * 4;

            readonly ISerializerSettings _configuration;
            readonly IObjectSerializer<T> _serializer;

            public ObjectDeserializer(ISerializerSettings config)
            {
                _configuration = config;
                _serializer = config.SerializerFactory.Create<T>();
            }

            public PooledStream GetObjectData(T objectData)
            {
                var ms = StreamPool.Instance.CreateWritable(_currentMaxBufferSize);

                try
                {
                    var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                    _serializer.WriteData(objectData, objWriter);

                    return ms;
                }
                catch (NotSupportedException)
                {
                    ms.Dispose();

                    lock (_lockObj)
                    {
                        _currentMaxBufferSize = _currentMaxBufferSize << 1;

                        if (_currentMaxBufferSize > MaxBufferSize)
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                return GetObjectData(objectData);
            }
        }
    }
}