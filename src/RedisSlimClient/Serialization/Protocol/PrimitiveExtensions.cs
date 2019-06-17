﻿using System;
using System.Text;
using RedisSlimClient.Types;
using RedisSlimClient.Types.Primatives;

namespace RedisSlimClient.Serialization
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

        public static (ResponseType type, long length, int offset) ToResponseType(this IByteSequence data)
        {
            var type = (ResponseType)data.GetValue(0);

            if (type != ResponseType.StringType && type != ResponseType.ErrorType)
            {
                return (type, data.ToInteger(1), 0);
            }

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
            return long.Parse(ToAsciiString(data, offset));
        }

        public static string ToAsciiString(this IByteSequence data, int offset = 0)
        {
            return Encoding.ASCII.GetString(data.ToArray(offset));
        }
    }
}