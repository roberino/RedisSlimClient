using System;

namespace RedisSlimClient.Serialization
{
    class BinaryFormatter : IBinaryFormatter
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        BinaryFormatter()
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

        public byte[] ToBytes(bool data)
        {
            return BitConverter.GetBytes(data);
        }

        public bool ToBool(byte[] data)
        {
            return BitConverter.ToBoolean(data, 0);
        }

        public double ToDouble(byte[] data)
        {
            return BitConverter.ToDouble(data, 0);
        }

        public byte[] ToBytes(double value)
        {
            return BitConverter.GetBytes(value);
        }

        public decimal ToDecimal(byte[] data)
        {
            return (decimal)BitConverter.ToDouble(data, 0);
        }

        public byte[] ToBytes(decimal value)
        {
            return BitConverter.GetBytes((double)value);
        }
    }
}