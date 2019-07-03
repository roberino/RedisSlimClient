using RedisSlimClient.Serialization.CustomSerializers;
using RedisSlimClient.Serialization.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace RedisSlimClient.Serialization
{
    public class SerializerFactory : IObjectSerializerFactory
    {
        readonly IDictionary<Type, object> _knownSerializers = new Dictionary<Type, object>
        {
            [typeof(XDocument)] = new XDocumentSerializer(),
            [typeof(XmlDocument)] = new XmlDocumentSerializer(),
            [typeof(Stream)] = new StreamSerializer(),
            [typeof(IDictionary<string, object>)] = new DictionarySerializer<object>()
        };

        SerializerFactory()
        {
        }

        public static readonly SerializerFactory Instance = new SerializerFactory();

        public IObjectSerializer<T> Create<T>()
        {
            var type = typeof(T);
            var tc = Type.GetTypeCode(type);

            if (tc == TypeCode.Object)
            {
                if (_knownSerializers.TryGetValue(type, out var sz))
                {
                    return (IObjectSerializer<T>)sz;
                }

                return TypeProxy<T>.Instance;
            }

            return PrimativeSerializer.CreateSerializer<T>();
        }
    }
}