using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace RedisSlimClient.Serialization
{
    class ObjectWriter : IObjectWriter
    {
        readonly Stream _stream;
        readonly IObjectSerializerFactory _serializerFactory;
        readonly Encoding _textEncoding;

        public ObjectWriter(Stream stream, Encoding textEncoding = null, IObjectSerializerFactory serializerFactory = null)
        {
            _stream = stream;
            _serializerFactory = serializerFactory ?? SerializerFactory.Instance;
            _textEncoding = textEncoding ?? Encoding.UTF8;
        }

        public void BeginWrite(int itemCount)
        {
            _stream.WriteStartArray(itemCount);
        }

        public void WriteItem(string name, object data)
        {
        }

        public void WriteItem(string name, string data)
        {
            Write(name, TypeCode.String, SubType.None, _textEncoding.GetBytes(data));
        }

        public void WriteItem(string name, byte[] data)
        {
            Write(name, TypeCode.Object, SubType.ByteArray, data);
        }

        public void WriteItem<T>(string name, T data)
        {
            var serializer = _serializerFactory.Create<T>();

            Write(name, TypeCode.Object, SubType.None, output =>
            {
                serializer.WriteData(data, this);
            });
        }

        public void WriteItem<T>(string name, IEnumerable<T> data)
        {
            var serializer = _serializerFactory.Create<T>();

            Write(name, TypeCode.Object, SubType.Collection, output =>
            {
                var itemCount = data.Count();

                output.WriteStartArray(itemCount);

                foreach (var item in data)
                {
                    serializer.WriteData(item, this);
                }
            });
        }

        public void WriteItem(string name, DateTime data)
        {
            Write(name, TypeCode.DateTime, SubType.None,
                XmlConvert.ToString(data, XmlDateTimeSerializationMode.Utc));
        }

        public void WriteItem(string name, short data)
        {
            Write(name, TypeCode.Int16, SubType.None, data.ToString());
        }

        public void WriteItem(string name, int data)
        {
            Write(name, TypeCode.Int32, SubType.None, data.ToString());
        }

        public void WriteItem(string name, long data)
        {
            Write(name, TypeCode.Int64, SubType.None, data.ToString());
        }

        public void WriteItem(string name, char data)
        {
            Write(name, TypeCode.Char, SubType.None, data.ToString());
        }

        void Write(string name, TypeCode type, SubType subType, string data)
        {
            Write(name, type, subType, Encoding.ASCII.GetBytes(data));
        }

        void Write(string name, TypeCode type, SubType subType, byte[] data)
        {
            _stream.WriteStartArray(4);
            _stream.Write(name);
            _stream.Write((int)type);
            _stream.Write((int)subType);
            _stream.Write(data);
        }

        void Write(string name, TypeCode type, SubType subType, Action<Stream> content)
        {
            _stream.WriteStartArray(4);
            _stream.Write(name);
            _stream.Write((int)type);
            _stream.Write((int)subType);
            content(_stream);
        }
    }
}
