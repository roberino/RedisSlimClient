using System;
using System.Collections.Generic;
using System.IO;

namespace RedisTribute.Serialization
{
    public interface IObjectReader
    {
        void BeginRead(int itemCount);
        Stream Raw();
        bool ReadBool(string name);
        byte[] ReadBytes(string name);
        string ReadString(string name);
        DateTime ReadDateTime(string name);
        int ReadInt32(string name);
        long ReadInt64(string name);
        char ReadChar(string name);
        decimal ReadDecimal(string name);
        double ReadDouble(string name);
        float ReadFloat(string name);
        T ReadObject<T>(string name, T defaultValue);
        IEnumerable<T> ReadEnumerable<T>(string name, ICollection<T> defaultValue);

        void EndRead();
    }
}
