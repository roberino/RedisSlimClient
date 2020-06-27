using System;
using System.IO;
using System.Text;
using RedisTribute.Types.Primatives;

namespace RedisTribute.Serialization.CustomSerializers
{
    class StringableSerializer<T> : IObjectSerializer<T>
    {
        readonly Encoding _encoding;
        readonly Func<string, T> _parser;
        readonly Func<T, string> _formatter;

        public StringableSerializer(Encoding encoding, Func<string, T> parser, Func<T, string> formatter)
        {
            _encoding = encoding;
            _parser = parser;
            _formatter = formatter;
        }

        public T ReadData(IObjectReader reader, T defaultValue)
        {
            using (var data = reader.Raw())
            {
                if (data.CanSeek && data.Length == 0)
                {
                    return defaultValue;
                }

                if (data is PooledStream ps)
                {
                    var seg = ps.GetBuffer();
                    return _parser(_encoding.GetString(seg.Array, seg.Offset, seg.Count));
                }

                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);

                    return _parser(_encoding.GetString(ms.ToArray()));
                }
            }
        }

        public void WriteData(T instance, IObjectWriter writer)
        {
            var bytes = instance == null ? null : _encoding.GetBytes(_formatter(instance));

            writer.Raw(bytes);
        }
    }
}