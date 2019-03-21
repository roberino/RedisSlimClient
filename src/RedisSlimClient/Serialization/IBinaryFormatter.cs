using System;

namespace RedisSlimClient.Serialization
{
    internal interface IBinaryFormatter
    {
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
    }
}