using System.Text;

namespace RedisTribute.Serialization.CustomSerializers
{
    class StringSerializer : StringableSerializer<string>
    {
        public StringSerializer(Encoding encoding) : base(encoding, x => x, x => x)
        {
        }
    }
}
