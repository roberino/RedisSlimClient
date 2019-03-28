using System;
using System.Collections.Generic;

namespace RedisSlimClient.Serialization
{
    public interface IObjectReader
    {
        void BeginRead(int itemCount);

        string ReadString(string name);
        DateTime ReadDateTime(string name);
        int ReadInt32(string name);
        long ReadInt64(string name);
        char ReadChar(string name);
        T ReadObject<T>(string name);
        IEnumerable<T> ReadEnumerable<T>(string name);

        void EndRead();
    }
}
