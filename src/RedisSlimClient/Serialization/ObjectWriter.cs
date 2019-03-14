using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using RedisSlimClient.Types;

namespace RedisSlimClient.Serialization
{
    class ObjectWriter : IObjectWriter
    {
        private readonly Encoding _textEncoding;
        private readonly IList<(string name, TypeCode type, SubType subType, byte[] data)> _objectData;

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

        public void WriteItem(string name, int level, object data)
        {
        }

        public void WriteItem(string name, int level, string data)
        {
            Write(name, TypeCode.String, SubType.None, _textEncoding.GetBytes(data));
        }

        public void WriteItem(string name, int level, byte[] data)
        {
            Write(name, TypeCode.Object, SubType.ByteArray, data);
        }

        public void WriteItem(string name, int level, IEnumerable data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, int level, DateTime data)
        {
            Write(name, TypeCode.DateTime, SubType.None,
                Encoding.ASCII.GetBytes(XmlConvert.ToString(data, XmlDateTimeSerializationMode.Utc)));
        }

        public void WriteItem(string name, int level, short data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, int level, int data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, int level, long data)
        {
            throw new NotImplementedException();
        }

        public void WriteItem(string name, int level, char data)
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
