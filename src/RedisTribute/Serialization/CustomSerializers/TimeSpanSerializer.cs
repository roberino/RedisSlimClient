using System;
using System.Text;

namespace RedisTribute.Serialization.CustomSerializers
{
    class TimeSpanSerializer : IObjectSerializer<TimeSpan>
    {
        readonly StringSerializer _stringSerializer = new StringSerializer(Encoding.ASCII);

        public static readonly IObjectSerializer<TimeSpan> Instance = new TimeSpanSerializer();

        public TimeSpan ReadData(IObjectReader reader, TimeSpan defaultValue)
        {
            var str = _stringSerializer.ReadData(reader, defaultValue.ToString());

            return TimeSpan.Parse(str);
        }

        public void WriteData(TimeSpan instance, IObjectWriter writer)
        {
            _stringSerializer.WriteData(instance.ToString(), writer);
        }
    }
}
