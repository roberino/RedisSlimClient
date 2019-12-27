using RedisTribute.Serialization.CustomSerializers;
using RedisTribute.Serialization.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RedisTribute.Serialization
{
    public class SerializerFactory : IObjectSerializerFactory
    {
        readonly IDictionary<Type, object> _knownSerializers = new Dictionary<Type, object>
        {
            [typeof(XDocument)] = new XDocumentSerializer(),
            [typeof(XmlDocument)] = new XmlDocumentSerializer(),
            [typeof(Stream)] = new StreamSerializer(),
            [typeof(IDictionary<string, string>)] = new DictionarySerializer<string>(),
            [typeof(Dictionary<string, string>)] = new DictionarySerializer<string>(),
            [typeof(KeyValuePair<string, string>)] = new KeyValueSerializer<string>(),
            [typeof(string)] = new StringSerializer(Encoding.UTF8),
            [typeof(byte[])] = new ByteArraySerializer(),
            [typeof(TimeSpan)] = TimeSpanSerializer.Instance
        };

        SerializerFactory()
        {
        }

        public static readonly SerializerFactory Instance = new SerializerFactory();

        public IObjectSerializer<T> Create<T>()
        {
            var type = typeof(T);
            var tc = Type.GetTypeCode(type);

            if (tc == TypeCode.Object || tc == TypeCode.String)
            {
                if (_knownSerializers.TryGetValue(type, out var sz))
                {
                    return (IObjectSerializer<T>)sz;
                }

                if (!type.IsPublic)
                {
                    throw new ArgumentException($"Can't serialize private type: {type.FullName}");
                }

                return TypeProxy<T>.Instance;
            }

            if (type.IsEnum)
            {
                return EnumSerializer<T>.Instance;
            }

            return PrimativeSerializer.CreateSerializer<T>();
        }
    }
}