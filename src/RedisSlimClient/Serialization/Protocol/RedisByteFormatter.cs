using System;
using System.Linq;

namespace RedisSlimClient.Serialization.Protocol
{
    class RedisByteFormatter
    {
        private readonly Memory<byte> _memory;

        int _position;

        public RedisByteFormatter(Memory<byte> stream)
        {
            _memory = stream;
        }

        public int Write(object item)
        {
            var tc = Type.GetTypeCode(item.GetType());

            switch (tc)
            {
                case TypeCode.String:
                    return Write((string)item, true);
                case TypeCode.Int32:
                    return Write((int)item);
                case TypeCode.Int64:
                    return Write((long)item);
                case TypeCode.Object:
                    return Write((byte[])item);
                default:
                    throw new NotSupportedException(tc.ToString());
            }
        }

        public int Write(object[] data)
        {
            WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }

            return _position;
        }

        public int Write(string[] data)
        {
            WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }

            return _position;
        }

        public int Write(byte[][] data)
        {
            WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                Write(data[i]);
            }

            return _position;
        }

        public int WriteStartArray(int arrayLength)
        {
            Write(ResponseType.ArrayType);
            WriteRaw(arrayLength.ToString());
            return WriteEnd();
        }

        public int Write(byte[] data)
        {
            Write(ResponseType.BulkStringType);
            WriteRaw(data.Length.ToString());
            WriteEnd();

            for(var i = 0; i < data.Length; i++)
            {
                _memory.Span[_position++] = data[i];
            }

            return WriteEnd();
        }

        public int Write(long value)
        {
            Write(ResponseType.IntType);
            WriteRaw(value.ToString());
            return WriteEnd();
        }

        public int Write(string data, bool bulk = false)
        {
            if (bulk)
            {
                Write(ResponseType.BulkStringType);
                Write(data.Length.ToString());
                WriteEnd();
                WriteRaw(data);
                WriteEnd();
            }
            else
            {
                Write(ResponseType.StringType);
                WriteRaw(data);
                WriteEnd();
            }

            return _position;
        }

        int WriteRaw(string data)
        {
            return Write(data.Select(b => (byte)b).ToArray());
        }

        int WriteEnd()
        {
            _memory.Span[_position++] = (byte)'\r';
            _memory.Span[_position++] = (byte)'\n';
            return _position;
        }

        int Write(ResponseType responseType)
        {
            _memory.Span[_position++] = (byte)responseType;
            return _position;
        }
    }
}
