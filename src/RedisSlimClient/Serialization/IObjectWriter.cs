using System;
using System.Collections;

namespace RedisSlimClient.Serialization
{
    public interface IObjectWriter
    {
        void BeginWrite(int itemCount);
        void EndWrite();

        void WriteItem(string name, int level, object data);
        void WriteItem(string name, int level, string data);
        void WriteItem(string name, int level, byte[] data);
        void WriteItem(string name, int level, IEnumerable data);
        void WriteItem(string name, int level, DateTime data);
        void WriteItem(string name, int level, short data);
        void WriteItem(string name, int level, int data);
        void WriteItem(string name, int level, long data);
        void WriteItem(string name, int level, char data);
    }
}