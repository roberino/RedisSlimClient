using RedisTribute.Serialization.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RedisTribute.Serialization
{
    class ObjectWriter : IObjectWriter
    {
        readonly Stream _stream;
        readonly IBinaryFormatter _dataFormatter;
        readonly IObjectSerializerFactory _serializerFactory;
        readonly Encoding _textEncoding;

        bool _isSub;

        public ObjectWriter(Stream stream, 
            Encoding textEncoding = null,
            IBinaryFormatter dataFormatter = null,
            IObjectSerializerFactory serializerFactory = null)
        {
            _stream = stream;
            _dataFormatter = dataFormatter ?? BinaryFormatter.Default;
            _serializerFactory = serializerFactory ?? SerializerFactory.Instance;
            _textEncoding = textEncoding ?? Encoding.UTF8;
        }

        public void BeginWrite(int itemCount)
        {
            _isSub = true;
            _stream.WriteStartArray(itemCount);
        }

        public void Raw(byte[] data, int? length = null)
        {
            if (_isSub)
            {
                _stream.WriteBytes(data, length.GetValueOrDefault((data?.Length).GetValueOrDefault()));
                return;
            }

            if (data == null || (length.HasValue && length.Value == 0))
            {
                return;
            }

            _stream.Write(data, 0, length.GetValueOrDefault(data.Length));
        }

        public void WriteItem(string name, string data)
        {
            Write(name, TypeCode.String, SubType.None, data == null ? null : _textEncoding.GetBytes(data));
        }

        public void WriteNullable<T>(string name, T? data, string method) where T : struct
        {
            if (data.HasValue)
            {
                GetType()
                    .GetMethods()
                    .Where(m => m.Name == method && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(T))
                    .Single()
                    .Invoke(this, new object[] { name, data.Value });
            }
        }

        public void WriteItem(string name, TimeSpan data)
        {
            Write(name, TypeCode.String, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, byte[] data)
        {
            Write(name, TypeCode.Object, SubType.ByteArray, data);
        }

        public void WriteEnum<T>(string name, T data)
        {
            Write(name, TypeCode.String, SubType.None, Encoding.ASCII.GetBytes($"{data}"));
        }

        public void WriteItem<T>(string name, T data)
        {
            if (data == default)
            {
                return;
            }

            var serializer = _serializerFactory.Create<T>();

            Write(name, TypeCode.Object, SubType.None, output =>
            {
                serializer.WriteData(data, this);
            });
        }

        public void WriteItem<T>(string name, IEnumerable<T> data)
        {
            var type = typeof(T);
            var colType = data.GetType();
            var tc = Type.GetTypeCode(type);

            Write(name, TypeCode.Object, SubType.Collection, output =>
            {
                if (tc == TypeCode.Object)
                {
                    var serializer = _serializerFactory.Create<T>();

                    var itemCount = data.Count();

                    output.WriteStartArray(itemCount);

                    foreach (var item in data)
                    {
                        serializer.WriteData(item, this);
                    }
                }
                else
                {
                    if (tc == TypeCode.String)
                    {
                        if (colType.IsArray)
                        {
                            output.Write((string[])(object)data);
                        }
                        else
                        {
                            output.Write(data.Select(x => (string)(object)x).ToArray());
                        }
                    }
                    else
                    {
                        var converter = PrimativeSerializer.CreateConverter<T>();

                        output.Write(data.Select(x => converter.GetBytes(x)).ToArray());
                    }
                }
            });
        }

        public void WriteItem(string name, DateTime data)
        {
            Write(name, TypeCode.DateTime, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, short data)
        {
            Write(name, TypeCode.Int16, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, int data)
        {
            Write(name, TypeCode.Int32, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, long data)
        {
            Write(name, TypeCode.Int64, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, char data)
        {
            Write(name, TypeCode.Char, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, bool data)
        {
            Write(name, TypeCode.Char, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, decimal data)
        {
            Write(name, TypeCode.Decimal, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, double data)
        {
            Write(name, TypeCode.Double, SubType.None, _dataFormatter.ToBytes(data));
        }

        public void WriteItem(string name, float data)
        {
            Write(name, TypeCode.Single, SubType.None, _dataFormatter.ToBytes(data));
        }

        void Write(string name, TypeCode type, SubType subType, string data)
        {
            Write(name, type, subType, Encoding.ASCII.GetBytes(data));
        }

        void Write(string name, TypeCode type, SubType subType, byte[] data)
        {
            Write(name, type, subType, s => s.WriteBytes(data));
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
