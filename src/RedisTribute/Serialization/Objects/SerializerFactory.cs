using RedisTribute.Serialization.CustomSerializers;
using RedisTribute.Serialization.Emit;
using RedisTribute.Serialization.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace RedisTribute.Serialization
{
    public sealed class SerializerFactory : IObjectSerializerFactory
    {
        readonly IObjectSerializerFactory _innerImpl;

        SerializerFactory(IObjectSerializerFactory innerImpl)
        {
            _innerImpl = innerImpl;
        }

        public static readonly SerializerFactory Instance = CreateFactory(new InnerSerializerFactory());

        public static SerializerFactory CreateFactory(IObjectSerializerFactory innerFactory) => new SerializerFactory(innerFactory);

        public IObjectSerializer<T> Create<T>()
        {
            return SerializerExceptionDecorator<T>.Default(_innerImpl.Create<T>);
        }

        class InnerSerializerFactory : IObjectSerializerFactory
        {
            readonly IDictionary<Type, object> _knownSerializers = new Dictionary<Type, object>
            {
                [typeof(XDocument)] = new XDocumentSerializer(),
                [typeof(XNode)] = new XDocumentSerializer(),
                [typeof(XElement)] = new XDocumentSerializer(),
                [typeof(XmlDocument)] = new XmlDocumentSerializer(),
                [typeof(XmlElement)] = new XmlDocumentSerializer(),
                [typeof(Stream)] = new StreamSerializer(),
                [typeof(IDictionary<string, string>)] = new DictionarySerializer<string>(),
                [typeof(Dictionary<string, string>)] = new DictionarySerializer<string>(),
                [typeof(KeyValuePair<string, string>)] = new KeyValueSerializer<string>(),
                [typeof(string)] = new StringSerializer(Encoding.UTF8), // TODO: Use encoding from settings?
                [typeof(byte[])] = new ByteArraySerializer(),
                [typeof(TimeSpan)] = TimeSpanSerializer.Instance,
                [typeof(Uri)] = new StringableSerializer<Uri>(Encoding.UTF8, s => new Uri(s, UriKind.RelativeOrAbsolute), u => u.ToString())
            };

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

                    if (type.IsCollectionOrArray())
                    {
                        return EnumerableSerializerExtensions.Create<T>();
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
}