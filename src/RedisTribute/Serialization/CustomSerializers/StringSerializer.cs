using RedisTribute.Types.Primatives;
using System.IO;
using System.Text;

namespace RedisTribute.Serialization.CustomSerializers
{
    class StringSerializer : IObjectSerializer<string>
    {
        readonly Encoding _encoding;

        public StringSerializer(Encoding encoding)
        {
            _encoding = encoding;
        }

        public string ReadData(IObjectReader reader, string defaultValue)
        {
            using (var data = reader.Raw())
            {
                if (data.CanSeek && data.Length == 0)
                {
                    return null;
                }

                if (data is PooledStream ps)
                {
                    var seg = ps.GetBuffer();
                    return _encoding.GetString(seg.Array, seg.Offset, seg.Count);
                }

                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);

                    return _encoding.GetString(ms.ToArray());
                }
            }
        }

        public void WriteData(string instance, IObjectWriter writer)
        {
            var bytes = instance == null ? null : _encoding.GetBytes(instance);

            writer.Raw(bytes);
        }
    }
}
