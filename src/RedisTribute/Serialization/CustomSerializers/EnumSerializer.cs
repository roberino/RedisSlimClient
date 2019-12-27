using RedisTribute.Types.Primatives;
using System;
using System.IO;
using System.Text;

namespace RedisTribute.Serialization.CustomSerializers
{
    class EnumSerializer<T> : IObjectSerializer<T>
    {
        private EnumSerializer() { }

        public static readonly EnumSerializer<T> Instance = new EnumSerializer<T>();

        public T ReadData(IObjectReader reader, T defaultValue)
        {
            using (var data = reader.Raw())
            {
                if (data.CanSeek && data.Length == 0)
                {
                    return default;
                }

                string val = null;

                if (data is PooledStream ps)
                {
                    var seg = ps.GetBuffer();
                    val = Encoding.ASCII.GetString(seg.Array, seg.Offset, seg.Count);
                }

                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);

                    val = Encoding.ASCII.GetString(ms.ToArray());
                }

                if (string.IsNullOrEmpty(val))
                {
                    return defaultValue;
                }

                return (T)Enum.Parse(typeof(T), val);
            }
        }

        public void WriteData(T instance, IObjectWriter writer)
        {
            writer.Raw(Encoding.ASCII.GetBytes($"{instance}"));
        }
    }
}
