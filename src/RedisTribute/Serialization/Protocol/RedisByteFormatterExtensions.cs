using System;
using System.Buffers;
using System.IO;

namespace RedisTribute.Serialization.Protocol
{
    static class RedisByteFormatterExtensions
    {
        public static void Write(this Stream output, object item)
        {
            var tc = Type.GetTypeCode(item.GetType());

            switch (tc)
            {
                case TypeCode.String:
                    output.Write((string)item, true);
                    break;
                case TypeCode.Int32:
                    output.Write((int)item);
                    break;
                case TypeCode.Int64:
                    output.Write((long)item);
                    break;
                case TypeCode.Object:
                    if (item is ArraySegment<byte>)
                    {
                        output.WriteBytes((ArraySegment<byte>)item);
                    }
                    else
                    {
                        output.WriteBytes((byte[])item);
                    }
                    break;
                default:
                    throw new NotSupportedException(tc.ToString());
            }
        }

        public static void Write(this Stream output, object[] data)
        {
            output.WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                output.Write(data[i]);
            }
        }

        public static void Write(this Stream output, string[] data)
        {
            output.WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                output.Write(data[i]);
            }
        }

        public static void Write(this Stream output, byte[][] data)
        {
            output.WriteStartArray(data.Length);

            for (var i = 0; i < data.Length; i++)
            {
                output.WriteBytes(data[i]);
            }
        }

        public static void WriteStartArray(this Stream output, int arrayLength)
        {
            output.Write(ResponseType.ArrayType);
            output.WriteRaw(arrayLength.ToString());
            output.WriteEnd();
        }

        public static void WriteBytes(this Stream output, byte[] data, int? length = null)
        {
            output.Write(ResponseType.BulkStringType);
            output.WriteRaw(data == null ? "-1" : data.Length.ToString());
            output.WriteEnd();
            if (data != null)
            {
                output.Write(data, 0, length.GetValueOrDefault(data.Length));
                output.WriteEnd();
            }
        }

        public static void WriteBytes(this Stream output, ArraySegment<byte> segment)
        {
            output.Write(ResponseType.BulkStringType);
            output.WriteRaw(segment.Count.ToString());
            output.WriteEnd();
            output.Write(segment.Array, segment.Offset, segment.Count);
            output.WriteEnd();
        }

        public static void Write(this Stream output, long value)
        {
            output.Write(ResponseType.IntType);
            output.WriteRaw(value.ToString());
            output.WriteEnd();
        }

        public static void Write(this Stream output, string data, bool bulk = false)
        {
            if (bulk)
            {
                output.Write(ResponseType.BulkStringType);
                output.WriteRaw(data.Length.ToString());
                output.WriteEnd();
                output.WriteRaw(data);
                output.WriteEnd();
            }
            else
            {
                output.Write(ResponseType.StringType);
                output.WriteRaw(data);
                output.WriteEnd();
            }
        }

        static void WriteRaw(this Stream output, string data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                output.WriteByte((byte)data[i]);
            }
        }

        static void WriteEnd(this Stream output)
        {
            output.WriteByte((byte)'\r');
            output.WriteByte((byte)'\n');
        }

        static void Write(this Stream output, ResponseType responseType)
        {
            output.WriteByte((byte)responseType);
        }
    }
}
