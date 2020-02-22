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

        public static string AsRedisSortOrder(this SortOrder sort)
        {
            if (sort == Types.SortOrder.Descending)
            {
                return "DESC";
            }
            return "ASC";
        }

        public static Exception AsException(this RedisError err)
        {
            if (ObjectMovedException.TryParse(err.Message, out var ex))
            {
                return ex;
            }

            return new RedisServerException(err.Message);
        }
    }
}
