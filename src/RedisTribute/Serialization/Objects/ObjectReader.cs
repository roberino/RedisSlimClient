using RedisTribute.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RedisTribute.Serialization
{
    class ObjectReader : IObjectReader
    {
        readonly IObjectSerializerFactory _serializerFactory;
        readonly IEnumerator<RedisObjectPart> _enumerator;
        readonly Stream _rawData;
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
        public ObjectReader(IEnumerable<RedisObjectPart> objectStream,
            Stream rawData,
            Encoding encoding = null,
            IBinaryFormatter dataFormatter = null,
            IObjectSerializerFactory serializerFactory = null) :
            this(objectStream, encoding, dataFormatter, serializerFactory)
        {
            _rawData = rawData;
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

        public Stream Raw()
        {
            if (_rawData != null)
            {
                return _rawData;
            }

            var obj = ReadNext();

            var str = (RedisString)obj.ToObjects().Single();

            return str.AsStream();
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
            return ReadPrimativeValue(name, _dataFormatter.ToDateTime);
        }

        public TimeSpan ReadTimeSpan(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToTimeSpan);
        }

        public int ReadInt32(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToInt32);
        }

        public long ReadInt64(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToInt64);
        }

        public char ReadChar(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToChar);
        }

        public bool ReadBool(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToBool);
        }

        public decimal ReadDecimal(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToDecimal);
        }

        public double ReadDouble(string name)
        {
            return ReadPrimativeValue(name, _dataFormatter.ToDouble);
        }

        public float ReadFloat(string name)
        {
            return (float)ReadPrimativeValue(name, _dataFormatter.ToDouble);
        }

        public T ReadEnum<T>(string name, T defaultValue)
        {
            return ReadPrimativeValue(name, x =>
            {
                return (x == null || x.Length == 0) ? defaultValue : (T)Enum.Parse(typeof(T), Encoding.ASCII.GetString(x));
            });
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
                        var str = (RedisString)e.Current.Value;

                        using (str)
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
                    var str = (RedisString)e.Current.Value;

                    using (str)
                        return converter.GetValue(str.Value);
                }

                return default;
            };
        }

        T ReadPrimativeValue<T>(string name, Func<byte[], T> converter)
        {
            var x = ReadStringProperty(name).Value;
            if (x == null)
            {
                return default;
            }
            return converter(x);
        }

        RedisString ReadStringProperty(string name)
        {
            var value = ReadSingleProperty(name);

            if (value.IsNull)
            {
                return new RedisString(null);
            }

            return (RedisString)value;
        }

        IRedisObject ReadSingleProperty(string name)
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
                    return RedisNull.Value;
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
