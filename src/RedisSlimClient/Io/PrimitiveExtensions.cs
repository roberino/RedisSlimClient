using System;
using System.Text;
using RedisSlimClient.Types;

namespace RedisSlimClient.Io
{
    internal static class PrimitiveExtensions
    {
        public static RedisType AsObjectType(this ResponseType type)
        {
            switch (type)
            {
                case ResponseType.BulkStringType:
                case ResponseType.StringType:
                    return RedisType.String;
                case ResponseType.IntType:
                    return RedisType.Integer;
                case ResponseType.ArrayType:
                    return RedisType.Array;
                default:
                    return RedisType.Null;
            }
        }

        public static (ResponseType type, long length, int offset) ToResponseType(this ArraySegment<byte> data)
        {
            var type = (ResponseType)data.Array[data.Offset];

            if (type != ResponseType.StringType && type != ResponseType.ErrorType)
            {
                return (type, data.ToInteger(1), 0);
            }

            return (type, data.Count - 1, 1);
        }

        public static byte[] ToBytes(this ArraySegment<byte> data, int offset = 0)
        {
            var bytes = new byte[data.Count - offset];

            Array.Copy(data.Array, (data.Offset + offset), bytes, 0, bytes.Length);

            return bytes;
        }

        public static long ToInteger(this ArraySegment<byte> data, int offset = 0)
        {
            return long.Parse(ToAsciiString(data, offset));
        }

        public static string ToAsciiString(this ArraySegment<byte> data, int offset = 0)
        {
            return Encoding.ASCII.GetString(data.Array, data.Offset + offset, data.Count - offset);
        }
    }
}