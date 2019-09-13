using RedisTribute.Types.Primatives;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RedisTribute.Serialization.Protocol
{
    class RedisByteFormatter
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
                    return Write((byte[])item);
                default:
                    throw new NotSupportedException(tc.ToString());
            }
        }

        public async ValueTask Write(object[] data)
        {
            await WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                await Write(data[i]);
            }
        }

        public async ValueTask Write(string[] data)
        {
            await WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                await Write(data[i]);
            }
        }

        public async ValueTask Write(byte[][] data)
        {
            await WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                await Write(data[i]);
            }
        }

        public async ValueTask WriteStartArray(int arrayLength)
        {
            await Write(ResponseType.ArrayType);
            await WriteRaw(arrayLength.ToString());
            await WriteEnd();
        }

        public async ValueTask Write(byte[] data)
        {
            await Write(ResponseType.BulkStringType);
            await WriteRaw(data.Length.ToString());
            await WriteEnd();

            await _memory.Write(data);

            await WriteEnd();
        }

        public async ValueTask Write(long value)
        {
            await Write(ResponseType.IntType);
            await WriteRaw(value.ToString());
            await WriteEnd();
        }

        public async ValueTask Write(string data, bool bulk = false)
        {
            if (bulk)
            {
                await Write(ResponseType.BulkStringType);
                await WriteRaw(data.Length.ToString());
                await WriteEnd();
                await WriteRaw(data);
                await WriteEnd();
            }
            else
            {
                await Write(ResponseType.StringType);
                await WriteRaw(data);
                await WriteEnd();
            }
        }

        ValueTask<bool> WriteRaw(string data)
        {
            return _memory.Write(Encoding.ASCII.GetBytes(data));
        }

        ValueTask<bool> WriteEnd()
        {
            return _memory.Write(_endBytes);
        }

        ValueTask<bool> Write(ResponseType responseType)
        {
            return _memory.Write(new byte[] { (byte)responseType });
        }
    }
}