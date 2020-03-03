using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RedisTribute.Configuration;
using RedisTribute.Types.Primatives;

namespace RedisTribute.Serialization.Objects
{
    class DictionaryObjectWriter : IObjectWriter
    {
        readonly ISerializerSettings _serializerSettings;
        readonly IBinaryFormatter _binaryFormatter;

        public DictionaryObjectWriter(ISerializerSettings serializerSettings, IBinaryFormatter binaryFormatter = null)
        {
            _serializerSettings = serializerSettings;
            _binaryFormatter = binaryFormatter ?? BinaryFormatter.Default;

            Output = new Dictionary<byte[], byte[]>(16);
        }

        public IDictionary<byte[], byte[]> Output { get; }

        public void BeginWrite(int itemCount)
        {
            throw new NotImplementedException();
        }

        void Write(string name, byte[] data)
        {
            Output[Encoding.UTF8.GetBytes(name)] = data;
        }

        public void Raw(byte[] data, int? length = null)
        {
            if (length.HasValue)
            {
                throw new NotSupportedException();
            }

            Write("$data", data);
        }

        public void WriteNullable<T>(string name, T? data, string onValue) where T : struct
        {
            throw new NotImplementedException();
        }

        public void WriteEnum<T>(string name, T data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem<T>(string name, IEnumerable<T> data)
        {
            var bytes = _serializerSettings.SerializeAsBytes(data);

            Write(name, bytes);
        }

        public void WriteItem<T>(string name, T data)
        {
            var bytes = _serializerSettings.SerializeAsBytes(data);

            Write(name, bytes);
        }

        public void WriteItem(string name, string data)
            => Write(name, _serializerSettings.Encoding.GetBytes(data));

        public void WriteItem(string name, byte[] data)
            => Write(name, data);

        public void WriteItem(string name, DateTime data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, TimeSpan data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, short data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, int data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, long data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, char data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, bool data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, decimal data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, double data)
            => Write(name, _binaryFormatter.ToBytes(data));

        public void WriteItem(string name, float data)
            => Write(name, _binaryFormatter.ToBytes(data));
    }
}
