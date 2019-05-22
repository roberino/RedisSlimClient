using System;
using System.IO;

namespace RedisSlimClient.Serialization.CustomSerializers
{
    class StreamSerializer : IObjectSerializer<Stream>
    {
        const string ItemName = nameof(Stream);

        public Stream ReadData(IObjectReader reader, Stream defaultValue)
        {
            var data = reader.ReadBytes(ItemName);

            if (!(defaultValue != null && defaultValue.CanWrite && defaultValue.CanRead && defaultValue.CanSeek))
            {
                defaultValue = new MemoryStream();
            }
            else
            {
                defaultValue.Position = 0;
            }

            defaultValue.Read(data, 0, data.Length);

            return defaultValue;
        }

        public void WriteData(Stream instance, IObjectWriter writer)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.CanRead && instance.CanWrite && instance.CanSeek)
            {
                if (instance is MemoryStream ms)
                {
                    writer.WriteItem(ItemName, ms.ToArray());
                }
                else
                {
                    var pos = instance.Position;

                    using (ms = new MemoryStream())
                    {
                        instance.CopyTo(ms);
                    }

                    instance.Position = pos;
                }
            }
            else
            {
                throw new NotSupportedException(instance.GetType().FullName);
            }
        }
    }
}