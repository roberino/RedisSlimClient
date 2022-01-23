using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RedisTribute.Configuration;
using RedisTribute.Types;

namespace RedisTribute.Serialization.Objects
{
    readonly struct DictionaryObjectReader : IObjectReader
    {
        readonly ISerializerSettings _serializerSettings;
        readonly IBinaryFormatter _binaryFormatter;
        readonly IDictionary<RedisKey, RedisKey> _input;

        public DictionaryObjectReader(IDictionary<RedisKey, RedisKey> input, ISerializerSettings serializerSettings, IBinaryFormatter? binaryFormatter = null)
        {
            _serializerSettings = serializerSettings;
            _binaryFormatter = binaryFormatter ?? BinaryFormatter.Default;
            _input = input;
        }

        public void BeginRead(int itemCount)
        {
        }

        public Stream Raw()
        {
            if (_input.TryGetValue("$data", out var data))
            {
                return new MemoryStream(data);
            }

            return Stream.Null;
        }

        public bool ReadBool(string name)
            => ReadOrDefault(name, _binaryFormatter.ToBool);

        public byte[] ReadBytes(string name)
            => ReadOrDefault(name, x => x);

        public string ReadString(string name)
            => ReadOrDefault(name, _serializerSettings.Encoding.GetString);

        public DateTime ReadDateTime(string name)
            => ReadOrDefault(name, _binaryFormatter.ToDateTime);

        public TimeSpan ReadTimeSpan(string name)
            => ReadOrDefault(name, _binaryFormatter.ToTimeSpan);

        public int ReadInt32(string name)
            => ReadOrDefault(name, _binaryFormatter.ToInt32);

        public long ReadInt64(string name)
            => ReadOrDefault(name, _binaryFormatter.ToInt64);

        public char ReadChar(string name)
            => ReadOrDefault(name, _binaryFormatter.ToChar);

        public decimal ReadDecimal(string name)
            => ReadOrDefault(name, _binaryFormatter.ToDecimal);

        public double ReadDouble(string name)
            => ReadOrDefault(name, _binaryFormatter.ToDouble);

        public float ReadFloat(string name)
        {
            var bf = _binaryFormatter;
            return ReadOrDefault(name, x => (float)bf.ToDouble(x));
        }

        public T ReadEnum<T>(string name, T defaultValue)
        {
            return ReadOrDefault(name, x =>
            {
                if (x.Length <= 0) 
                    return default(T);

                return (T)Enum.Parse(typeof(T), Encoding.ASCII.GetString(x));

            });
        }

        public T ReadObject<T>(string name, T defaultValue)
        {
            return ReadOrDefault(name, _serializerSettings.Deserialize<T>);
        }

        public IEnumerable<T> ReadEnumerable<T>(string name, ICollection<T> defaultValue)
        {
            return ReadOrDefault(name, _serializerSettings.Deserialize<List<T>>, defaultValue);
        }

        public void EndRead()
        {
        }

        T ReadOrDefault<T>(string name, Func<byte[], T> parser, T defaultValue = default)
        {
            if (_input.TryGetValue(name, out var data))
            {
                return parser(data);
            }

            return defaultValue;
        }
    }
}
