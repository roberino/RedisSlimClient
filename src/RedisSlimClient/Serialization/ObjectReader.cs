using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisSlimClient.Serialization
{
    class ObjectReader : IObjectReader
    {
        readonly IObjectSerializerFactory _serializerFactory;
        readonly IEnumerator<RedisObjectPart> _enumerator;
        readonly IBinaryFormatter _dataFormatter;
        readonly Encoding _encoding;

        readonly IDictionary<string, IList<RedisObjectPart>> _buffer;

        readonly int _level;

        public ObjectReader(IEnumerable<RedisObjectPart> objectStream,
            Encoding encoding = null,
            IBinaryFormatter dataFormatter = null,
            IObjectSerializerFactory serializerFactory = null) :
            this(objectStream.GetEnumerator(), 0, encoding, dataFormatter, serializerFactory)
        {
        }

        ObjectReader(IEnumerator<RedisObjectPart> objectStream,
            int level,
            Encoding encoding = null,
            IBinaryFormatter dataFormatter = null,
            IObjectSerializerFactory serializerFactory = null)
        {
            _enumerator = objectStream;
            _dataFormatter = dataFormatter ?? BinaryFormatter.Default;
            _encoding = encoding ?? Encoding.UTF8;
            _serializerFactory = serializerFactory ?? SerializerFactory.Instance;
            _buffer = new Dictionary<string, IList<RedisObjectPart>>();
            _level = level;
        }

        public void BeginRead(int itemCount)
        {
        }

        public void EndRead()
        {
            _buffer.Clear();
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
            var sz = _serializerFactory.Create<T>();

            return ReadToProperty(name, e =>
            {
                var subReader = new ObjectReader(e, _level + 1, _encoding, _dataFormatter);

                return sz.ReadData(subReader);
            });
        }

        public IEnumerable<T> ReadEnumerable<T>(string name)
        {
            throw new NotImplementedException();
        }

        RedisString ReadStringProperty(string name)
        {
            return (RedisString)ReadSingleProperty(name);
        }

        RedisObject ReadSingleProperty(string name)
        {
            if (_buffer.TryGetValue(name, out var parts))
            {
                return parts.Single().Value;
            }

            while (true)
            {
                var next = ReadNextProperty();

                if (string.Equals(next.name, name))
                {
                    return Read(1)[0].Value;
                }

                _buffer[next.name] = ReadNext();
            }
        }

        T ReadToProperty<T>(string name, Func<IEnumerator<RedisObjectPart>, T> dataReader)
        {
            if (_buffer.TryGetValue(name, out var parts))
            {
                return dataReader.Invoke(((IEnumerable<RedisObjectPart>)parts).GetEnumerator());
            }

            while (true)
            {
                var next = ReadNextProperty();

                if (string.Equals(next.name, name))
                {
                    return dataReader.Invoke(_enumerator);
                }

                _buffer[next.name] = ReadNext();
            }
        }

        IList<RedisObjectPart> ReadNext()
        {
            var first = true;
            var level = -1;

            var items = new List<RedisObjectPart>();

            while (_enumerator.MoveNext() && _enumerator.Current.Level >= level)
            {
                if (first)
                {
                    first = false;

                    if (!_enumerator.Current.IsArrayStart)
                    {
                        items.Add(_enumerator.Current);
                        break;
                    }

                    level = _enumerator.Current.Level;
                }

                items.Add(_enumerator.Current);
            }

            return items;
        }

        RedisObjectPart[] Read(int count)
        {
            var items = new RedisObjectPart[count];

            var i = 0;

            while (_enumerator.MoveNext())
            {
                if (_enumerator.Current.IsArrayStart)
                {
                    continue;
                }

                items[i++] = _enumerator.Current;

                if (i == count)
                {
                    break;
                }
            }

            return items;
        }

        (string name, TypeCode type, SubType subType) ReadNextProperty()
        {
            while (_enumerator.MoveNext())
            {
                if (_enumerator.Current.IsArrayStart && _enumerator.Current.Level == (_level + 1))
                {
                    break;
                }
            }

            var nextTuple = Read(3);

            return (nextTuple[0].Value.ToString(), (TypeCode)nextTuple[1].Value.ToLong(), (SubType)nextTuple[2].Value.ToLong());
        }
    }
}
