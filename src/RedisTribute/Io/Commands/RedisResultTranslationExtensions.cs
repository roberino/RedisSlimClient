using RedisTribute.Types;
using System;

namespace RedisTribute.Io.Commands
{
    static class RedisResultTranslationExtensions
    {
        public const string SuccessResponse = "OK";

        public static bool IsOk(this IRedisObject value)
        {
            return value != null && !value.IsNull && value.Type == RedisType.String && string.Equals(value.ToString(), SuccessResponse, StringComparison.OrdinalIgnoreCase);
        }
    }
}
