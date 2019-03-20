using System;

namespace RedisSlimClient.Serialization
{
    public interface IObjectReader
    {
        string ReadString(string name);
        DateTime ReadDateTime(string name);
        int ReadInt32(string name);
        long ReadInt64(string name);
        char ReadChar(string name);

        void EndRead();
    }
}
