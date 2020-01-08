using System;
using System.Text;
using RedisTribute.Types;
using RedisTribute.Types.Primatives;
using RedisTribute.Util;

namespace RedisTribute.Serialization
{
    static class PrimitiveExtensions
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

        public static (ResponseType type, long length, int offset) ToResponseType(this IByteSequence data)
        {
            var type = (ResponseType)data.GetValue(0);

            if (type == ResponseType.ArrayType || type == ResponseType.BulkStringType || type == ResponseType.IntType)
            {
                return (type, data.ToInteger(1), 0);
            }

            DebugOutput.Dump(data.ToArray(), data.Length);

            return (type, data.Length - 1, 1);
        }

        public static byte[] ToBytes(this ArraySegment<byte> data, int offset = 0)
        {
            var bytes = new byte[data.Count - offset];

            Array.Copy(data.Array, (data.Offset + offset), bytes, 0, bytes.Length);

            return bytes;
        }

        public static long ToInteger(this IByteSequence data, int offset = 0)
        {
            var strItem = ToAsciiString(data, offset);
            return long.Parse(strItem);
        }

        public static string ToAsciiString(this IByteSequence data, int offset = 0)
        {
            return Encoding.ASCII.GetString(data.ToArray(offset));
        }
    }
}