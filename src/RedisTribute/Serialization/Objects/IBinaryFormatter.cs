using System;

namespace RedisTribute.Serialization
{
    interface IBinaryFormatter
    {
        double ToDouble(byte[] data);
        byte[] ToBytes(double value);
        decimal ToDecimal(byte[] data);
        byte[] ToBytes(decimal value);
        int ToInt32(byte[] data);
        byte[] ToBytes(int value);
        long ToInt64(byte[] data);
        byte[] ToBytes(long value);
        short ToInt16(byte[] data);
        byte[] ToBytes(short value);
        char ToChar(byte[] data);
        byte[] ToBytes(char data);
        DateTime ToDateTime(byte[] data);
        byte[] ToBytes(DateTime date);
        TimeSpan ToTimeSpan(byte[] data);
        byte[] ToBytes(TimeSpan time);
        byte[] ToBytes(bool data);
        bool ToBool(byte[] data);
    }
}