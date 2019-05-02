using System;

namespace RedisSlimClient.Serialization
{
    class BinaryFormatter : IBinaryFormatter
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private BinaryFormatter()
        {
        }

        public static readonly IBinaryFormatter Default = new BinaryFormatter();

        public int ToInt32(byte[] data) => BitConverter.ToInt32(data, 0);

        public byte[] ToBytes(int value) => BitConverter.GetBytes(value);

        public long ToInt64(byte[] data) => BitConverter.ToInt64(data, 0);

        public byte[] ToBytes(long value) => BitConverter.GetBytes(value);

        public short ToInt16(byte[] data) => BitConverter.ToInt16(data, 0);

        public byte[] ToBytes(short value) => BitConverter.GetBytes(value);

        public char ToChar(byte[] data) => BitConverter.ToChar(data, 0);

        public byte[] ToBytes(char value) => BitConverter.GetBytes(value);

        public DateTime ToDateTime(byte[] data)
        {
            var value = BitConverter.ToInt64(data, 0);

            return DateTime.FromBinary(value);
        }

        public byte[] ToBytes(DateTime date)
        {
            return BitConverter.GetBytes(date.ToBinary());
        }
    }
}