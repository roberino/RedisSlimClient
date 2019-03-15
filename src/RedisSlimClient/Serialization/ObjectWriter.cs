using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace RedisSlimClient.Serialization
{
    internal class ObjectWriter : IObjectWriter
    {
        readonly Encoding _textEncoding;
        readonly IList<(string name, TypeCode type, SubType subType, byte[] data)> _objectData;

        public ObjectWriter(Encoding textEncoding = null)
        {
            _textEncoding = textEncoding ?? Encoding.UTF8;
            _objectData = new List<(string name, TypeCode type, SubType subType, byte[] data)>();
        }

        public void BeginWrite(int itemCount)
        {

        }

        public void EndWrite()
        {

        }

        public void WriteItem(string name, object data)
        {
        }

        public void WriteItem(string name, string data)
        {
            Write(name, TypeCode.String, SubType.None, _textEncoding.GetBytes(data));
        }

        public void WriteItem(string name, byte[] data)
        {
            Write(name, TypeCode.Object, SubType.ByteArray, data);
        }

        public void WriteItem<T>(string name, IEnumerable<T> data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, DateTime data)
        {
            Write(name, TypeCode.DateTime, SubType.None,
                Encoding.ASCII.GetBytes(XmlConvert.ToString(data, XmlDateTimeSerializationMode.Utc)));
        }

        public void WriteItem(string name, short data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, int data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, long data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, char data)
        {
            throw new NotImplementedException();
        }

        void Write(string name, TypeCode type, SubType subType, byte[] data)
        {
            _objectData.Add((name, type, subType, data));
        }

        public void Output(Stream output)
        {
            foreach (var item in _objectData)
            {
                output.Write(item.data, 0, item.data.Length);
            }
        }
    }

    public enum SubType
    {
        None,
        Collection,
        ByteArray
    }
}
