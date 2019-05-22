using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public void Dump(Action<string> writer)
        {
            while (_enumerator.MoveNext())
            {
                var item = _enumerator.Current;
                var pad = "".PadLeft(item.Level);
                writer($"{item.Level}{pad}arr:{item.IsArrayStart},len{item.Length},{item.Value}");
            }
        }

        public void BeginRead(int itemCount)
        {
            var arr = MoveNextArray();

            if (arr.dim != itemCount)
            {
                Trace.WriteLine($"exp:{itemCount}/act:{arr}");
            }
        }

        public void EndRead()
        {
            _buffer.Clear();
        }

        public byte[] Raw()
        {
            var obj = ReadNext();

            return ((RedisString)obj.ToObjects().Single()).Value;
        }

        public string ReadString(string name)
        {
            return ReadStringProperty(name).ToString(_encoding);
        }

        public byte[] ReadBytes(string name)
        {
            return ReadStringProperty(name).Value;
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

        public bool ReadBool(string name)
        {
            return _dataFormatter.ToBool(ReadStringProperty(name).Value);
        }

        public decimal ReadDecimal(string name)
        {
            return _dataFormatter.ToDecimal(ReadStringProperty(name).Value);
        }

        public double ReadDouble(string name)
        {
            return _dataFormatter.ToDouble(ReadStringProperty(name).Value);
        }

        public float ReadFloat(string name)
        {
            return (float)_dataFormatter.ToDouble(ReadStringProperty(name).Value);
        }

        public T ReadObject<T>(string name, T defaultValue)
        {
            var sz = _serializerFactory.Create<T>();

            return ReadToProperty(name, e =>
            {
                if (e == null)
                {
                    return default;
                }

                var subReader = new ObjectReader(e, _level + 1, _encoding, _dataFormatter);

                return sz.ReadData(subReader, defaultValue);
            });
        }

        public IEnumerable<T> ReadEnumerable<T>(string name, ICollection<T> defaultValue)
        {
            return ReadToProperty(name, e =>
            {
                var arrayDim = MoveNextArray();
                var itemReader = CreateItemReader<T>(e, arrayDim.level);

                if (defaultValue != null && !defaultValue.IsReadOnly)
                {
                    for (var x = 0; x < arrayDim.dim; x++)
                    {
                        defaultValue.Add(itemReader());
                    }

                    return defaultValue;
                }
                else
                {
                    var results = new T[arrayDim.dim];

                    for (var x = 0; x < arrayDim.dim; x++)
                    {
                        results[x] = itemReader();
                    }

                    return results;
                }
            });
        }

        Func<T> CreateItemReader<T>(IEnumerator<RedisObjectPart> e, int level)
        {
            var type = typeof(T);
            var tc = Type.GetTypeCode(type);

            if (tc == TypeCode.Object)
            {
                var sz = _serializerFactory.Create<T>();
                var subReader = new ObjectReader(e, level, _encoding, _dataFormatter);

                return () => sz.ReadData(subReader, default);
            }

            if (tc == TypeCode.String)
            {
                return () =>
                {
                    if (e.MoveNext())
                    {
                        var str = e.Current.Value as RedisString;

                        return (T)(object)str.ToString(_encoding);
                    }

                    return default;
                };
            }

            var converter = PrimativeSerializer.CreateConverter<T>();

            return () =>
            {
                if (e.MoveNext())
                {
                    var str = e.Current.Value as RedisString;

                    return converter.GetValue(str.Value);
                }

                return default;
            };
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

                if (next.eof)
                {
                    return null;
                }

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
                return dataReader.Invoke(parts.GetEnumerator());
            }

            while (true)
            {
                var next = ReadNextProperty();

                if (next.eof)
                {
                    return dataReader.Invoke(null);
                }

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

        (long dim, int level) MoveNextArray()
        {
            while (_enumerator.MoveNext())
            {
                if (_enumerator.Current.IsArrayStart)
                {
                    return (_enumerator.Current.Length, _enumerator.Current.Level);
                }
            }

            return (-1, 0);
        }

        RedisObjectPart[] ReadNextArray(long? maxCount = null)
        {
            var actualCount = MoveNextArray().dim;

            if (actualCount < 0)
            {
                return null;
            }

            var count = maxCount.HasValue ? Math.Min(maxCount.Value, actualCount) : actualCount;
            var items = new RedisObjectPart[count];

            var i = 0;

            while (_enumerator.MoveNext())
            {
                if (_enumerator.Current.IsArrayStart)
                {
                    throw new InvalidOperationException();
                }

                items[i++] = _enumerator.Current;

                if (i == items.Length)
                {
                    break;
                }
            }

            return items;
        }

        (string name, TypeCode type, SubType subType, bool eof) ReadNextProperty()
        {
            var nextTuple = ReadNextArray(3);

            if (nextTuple == null || nextTuple.Length <= 0 || nextTuple[0].IsEmpty)
            {
                return (null, TypeCode.Empty, SubType.None, true);
            }

            return (nextTuple[0].Value.ToString(), (TypeCode)nextTuple[1].Value.ToLong(), (SubType)nextTuple[2].Value.ToLong(), false);
        }
    }
}
