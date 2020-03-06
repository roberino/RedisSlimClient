using RedisTribute.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using RedisTribute.Types;

namespace RedisTribute.Serialization.Objects
{
    class DictionaryObjectWriter : IObjectWriter
    {
        readonly ISerializerSettings _serializerSettings;
        readonly IBinaryFormatter _binaryFormatter;

        IDictionary<RedisKey, RedisKey> _output;

        public DictionaryObjectWriter(ISerializerSettings serializerSettings, IBinaryFormatter binaryFormatter = null)
        {
            _serializerSettings = serializerSettings;
            _binaryFormatter = binaryFormatter ?? BinaryFormatter.Default;
            _output = null;
        }

        public IDictionary<RedisKey, RedisKey> Output
        {
            get
            {
                if (_output == null)
                {
                    _output = new Dictionary<RedisKey, RedisKey>(16);
                }

                return _output;
            }
        }

        public void BeginWrite(int itemCount)
        {
            _output = new Dictionary<RedisKey, RedisKey>(itemCount);
        }

        public void Raw(byte[] data, int? length = null)
        {
            if (length.HasValue)
            {
                var buf = new byte[length.Value];

                Array.Copy(data, 0, buf, 0, length.Value);

                data = buf;
            }

            Write("$data", data);
        }

        public void WriteNullable<T>(string name, T? data, string method) where T : struct
        {
            if (data.HasValue)
            {
                // TODO: Make this simpler & faster

                this.BindToMethod(method, p => p.Length == 2 && p[0].ParameterType == typeof(T), name, data.Value);
            }
        }

        public void WriteEnum<T>(string name, T data)
        {
            Write(name, Encoding.ASCII.GetBytes($"{data}"));
        }

        public void WriteItem<T>(string name, IEnumerable<T> data)
        {
            if (data == null)
            {
                return;
            }

            var bytes = _serializerSettings.SerializeAsBytes(data);

            Write(name, bytes);
        }

        public void WriteItem<T>(string name, T data)
        {
            if (data == null)
            {
                return;
            }

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
        
        void Write(string name, byte[] data)
            => Output[Encoding.UTF8.GetBytes(name)] = data;
    }
}
