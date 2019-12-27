using RedisTribute.Types.Primatives;
using System.IO;

namespace RedisTribute.Serialization.CustomSerializers
{
    class ByteArraySerializer : IObjectSerializer<byte[]>
    {
        public byte[] ReadData(IObjectReader reader, byte[] defaultValue)
        {
            using (var data = reader.Raw())
            {
                if (data is PooledStream ps)
                {
                    var seg = ps.GetBuffer();
                    return seg.ToBytes();
                }

                using (var ms = new MemoryStream())
                {
                    data.CopyTo(ms);

                    return ms.ToArray();
                }
            }
        }

        public void WriteData(byte[] instance, IObjectWriter writer)
        {
            writer.Raw(instance);
        }
    }
}
