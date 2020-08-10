using RedisTribute.Types.Primatives;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RedisTribute.Serialization.Protocol
{
    readonly struct RedisByteFormatter
    {
        static readonly byte[] _endBytes = new byte[] { (byte)'\r', (byte)'\n' };

        readonly IMemoryCursor _memory;

        public RedisByteFormatter(IMemoryCursor memory)
        {
            _memory = memory;
        }

        public ValueTask Write(object item)
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
                    if (item is ArraySegment<byte>)
                    {
                        return Write((ArraySegment<byte>)item);
                    }

                    return Write((byte[])item);
                default:
                    throw new NotSupportedException(tc.ToString());
            }
        }

        public ValueTask Write(object[] data)
        {
            var task = WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                task = Write(data[i]);
            }

            return task;
        }

        public ValueTask Write(string[] data)
        {
            var task = WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                task = Write(data[i]);
            }

            return task;
        }

        public ValueTask Write(byte[][] data)
        {
            var task = WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                task = Write(data[i]);
            }

            return task;
        }

        public ValueTask WriteStartArray(int arrayLength)
        {
            Write(ResponseType.ArrayType);
            WriteRaw(arrayLength.ToString());
            return WriteEnd();
        }

        public ValueTask Write(byte[] data)
        {
            Write(ResponseType.BulkStringType);
            WriteRaw(data.Length.ToString());
            WriteEnd();

            _memory.Write(data);

            return WriteEnd();
        }

        public ValueTask Write(ArraySegment<byte> data)
        {
            Write(ResponseType.BulkStringType);
            WriteRaw(data.Count.ToString());
            WriteEnd();

            _memory.Write(data);

            return WriteEnd();
        }

        public ValueTask Write(long value)
        {
            Write(ResponseType.IntType);
            WriteRaw(value.ToString());
            return WriteEnd();
        }

        public ValueTask Write(string data, bool bulk = false)
        {
            if (bulk)
            {
                Write(ResponseType.BulkStringType);
                WriteRaw(data.Length.ToString());
                WriteEnd();
                WriteRaw(data);
            }
            else
            {
                Write(ResponseType.StringType);
                WriteRaw(data);
            }
            return WriteEnd();
        }

        ValueTask WriteRaw(string data) => _memory.Write(Encoding.ASCII.GetBytes(data));

        ValueTask WriteEnd() => _memory.Write(_endBytes);

        ValueTask Write(ResponseType responseType) => _memory.Write(new byte[] { (byte)responseType });
    }
}