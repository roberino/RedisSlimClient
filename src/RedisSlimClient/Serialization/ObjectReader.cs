using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisSlimClient.Serialization
{
    class ObjectReader : IObjectReader
    {
        readonly IEnumerator<RedisObjectPart> _enumerator;
        readonly IBinaryFormatter _dataFormatter;
        readonly IDictionary<string, RedisObjectPart[]> _cache;
        readonly Encoding _encoding;

        public ObjectReader(IEnumerable<RedisObjectPart> objectStream, 
            Encoding encoding = null,
            IBinaryFormatter dataFormatter = null)
        {
            _enumerator = objectStream.GetEnumerator();
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

        RedisString ReadStringProperty(string name)
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

        RedisObject[] Read(int count)
        {
            var items = new RedisObject[count];

            for (var i = 0; i < count; i++)
            {
                if (!_enumerator.MoveNext())
                {
                    break;
                }

                items[i] = _enumerator.Current.Value;
            }

            return items;
        }

        (string name, TypeCode type, SubType subType) ReadNextProperty()
        {
            var nextTuple = Read(3);

            return (nextTuple[0].ToString(), (TypeCode)nextTuple[1].ToLong(), (SubType)nextTuple[2].ToLong());
        }

        IEnumerable<RedisObjectPart> ReadNextObject()
        {
            var len = 0L;
            var i = 0;

            while (_enumerator.MoveNext())
            {
                var part = _enumerator.Current;

                if (len == 0 && part.ArrayIndex.HasValue)
                {
                    len = part.Length - part.ArrayIndex.Value;
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
