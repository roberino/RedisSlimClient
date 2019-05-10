using System;
using System.Collections.Generic;

namespace RedisSlimClient.Serialization
{
    public interface IObjectWriter
    {
        void BeginWrite(int itemCount);
        void WriteRaw(byte[] data);
        void WriteItem<T>(string name, IEnumerable<T> data);
        void WriteItem<T>(string name, T data);
        void WriteItem(string name, string data);
        void WriteItem(string name, byte[] data);
        void WriteItem(string name, DateTime data);
        void WriteItem(string name, short data);
        void WriteItem(string name, int data);
        void WriteItem(string name, long data);
        void WriteItem(string name, char data);
        void WriteItem(string name, bool data);
    }
}