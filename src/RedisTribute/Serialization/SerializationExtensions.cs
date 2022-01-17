using RedisTribute.Configuration;
using RedisTribute.Io;
using RedisTribute.Types;
using RedisTribute.Types.Primatives;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Xml;

namespace RedisTribute.Serialization
{
    public static class SerializationExtensions
    {
        public static string CreateHash(this byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                return Convert.ToBase64String(sha1.ComputeHash(data));
            }
        }

        public static bool Verify(this byte[] data, string hash)
            => string.Equals(data.CreateHash(), hash);
    }

    static class SerializationExtensionsInternal
    {
        public static string ToPrimativeString(this object value)
        {
            if (value == null)
            {
                return null;
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.DateTime:
                    return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.Utc);
                case TypeCode.Double:
                    return ((double)value).ToString("C");

            }

            return value.ToString();
        }

        public static bool AreBinaryEqual<T>(this ISerializerSettings serializerSettings, byte[] serializedData, T preSerializedValue)
        {
            var originalLocal = serializerSettings.SerializeAsBytes(preSerializedValue);
            return StructuralComparisons.StructuralEqualityComparer.Equals(serializedData, originalLocal);
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, IRedisObject result)
        {
            return Deserialize(serializerSettings, serializerSettings.SerializerFactory.Create<T>(), result);
        }

        public static T Deserialize<T>(this ISerializerSettings serializerSettings, byte[] data)
        {
            return Deserialize(serializerSettings, serializerSettings.SerializerFactory.Create<T>(), data);
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
            using (var ms = StreamPool.Instance.CreateReadonly(data))
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

                void DisposeAndIncrementBuffer(IDisposable disposable, Exception ex)
                {
                    disposable.Dispose();

                    lock (_lockObj)
                    {
                        var nextMaxBufferSize = _currentMaxBufferSize << 1;

                        if (nextMaxBufferSize > MaxBufferSize)
                        {
                            throw new NotSupportedException($"Max buffer size of {MaxBufferSize} bytes exceeded", ex);
                        }

                        _currentMaxBufferSize = nextMaxBufferSize;
                    }
                }

                try
                {
                    var objWriter = new ObjectWriter(ms, _configuration.Encoding, null, _configuration.SerializerFactory);

                    _serializer.WriteData(objectData, objWriter);

                    return ms;
                }
                catch (SerializationException ex) when  (ex.InnerException is NotSupportedException && ex.InnerException.Message.Contains("not expandable"))
                {
                    DisposeAndIncrementBuffer(ms, ex);
                }
                catch (NotSupportedException ex)
                {
                    DisposeAndIncrementBuffer(ms, ex);
                }

                return GetObjectData(objectData);
            }
        }
    }
}