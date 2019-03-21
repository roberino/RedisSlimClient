using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisSlimClient.Serialization
{
    class ObjectReader : IObjectReader
    {
        readonly IEnumerable<RedisObjectPart> _enumerator;
        readonly IBinaryFormatter _dataFormatter;
        readonly IDictionary<string, RedisObjectPart[]> _cache;
        readonly Encoding _encoding;

        public ObjectReader(IEnumerable<RedisObjectPart> objectStream, 
            Encoding encoding = null,
            IBinaryFormatter dataFormatter = null)
        {
            _enumerator = objectStream;
            _dataFormatter = dataFormatter ?? BinaryFormatter.Default;
            _encoding = encoding ?? Encoding.UTF8;
            _cache = new Dictionary<string, RedisObjectPart[]>();
        }

        public string ReadString(string name)
        {
            return ReadStringProperty(name).ToString(_encoding);
        }

        public DateTime ReadDateTime(string name)
        {
            return _dataFormatter.ToDateTime(ReadStringProperty(name).Value);
        }

        public int ReadInt32(string name)
        {
            return _dataFormatter.ToInt32(ReadStringProperty(name).Value);
        }

        public long ReadInt64(string name)
        {
            return _dataFormatter.ToInt64(ReadStringProperty(name).Value);
        }

        public char ReadChar(string name)
        {
            return _dataFormatter.ToChar(ReadStringProperty(name).Value);
        }

        public T ReadObject<T>(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> ReadEnumerable<T>(string name)
        {
            throw new NotImplementedException();
        }

        public void EndRead()
        {
        }

        public RedisString ReadStringProperty(string name)
        {
            return (RedisString)ReadProperty(name).Single().Value;
        }

        IEnumerable<RedisObjectPart> ReadProperty(string name)
        {
            if (_cache.TryGetValue(name, out var parts))
            {
                return parts;
            }

            while (true)
            {
                var next = ReadNextProperty();

                if (string.Equals(next.name, name))
                {
                    return ReadNextObject();
                }

                _cache[next.name] = ReadNextObject().ToArray();
            }
        }

        (string name, TypeCode type, SubType subType) ReadNextProperty()
        {
            var nextTuple = _enumerator.Take(3).Select(x => x.Value).ToArray();

            return (nextTuple[0].ToString(), (TypeCode)nextTuple[1].ToLong(), (SubType)nextTuple[2].ToLong());
        }

        IEnumerable<RedisObjectPart> ReadNextObject()
        {
            long len = 0;
            int i = 0;

            foreach (var part in _enumerator)
            {
                if (len == 0)
                {
                    len = part.Length;
                }

                yield return part;

                if (i >= len)
                {
                    break;
                }

                i++;
            }
        }
    }
}
