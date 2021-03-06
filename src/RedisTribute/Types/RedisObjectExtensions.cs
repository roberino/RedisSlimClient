﻿using System.Collections.Generic;

namespace RedisTribute.Types
{
    static class RedisObjectExtensions
    {
        public static IRedisObject ReadObject(this IEnumerator<RedisObjectPart> partEnumerator)
        {
            // e.g.
            // array:start:L1
            // string:value:L1
            // string:value:L1
            //      array:start:L2
            //      int:value:L2
            // string:value:0

            var builder = new ArrayBuilder();

            while (partEnumerator.MoveNext())
            {
                var next = builder.AddOrYield(partEnumerator.Current);

                if (next != null)
                {
                    return next;
                }
            }

            return null;
        }

        public static IEnumerable<IRedisObject> ToObjects(this IEnumerable<RedisObjectPart> parts)
        {
            // e.g.
            // array:start:L1
            // string:value:L1
            // string:value:L1
            //      array:start:L2
            //      int:value:L2
            // string:value:0


            var builder = new ArrayBuilder();

            foreach (var part in parts)
            {
                var next = builder.AddOrYield(part);

                if (next != null)
                {
                    yield return next;
                }
            }
        }

        public static long ToLong(this IRedisObject value)
        {
            if (value is RedisInteger i)
            {
                return i.Value;
            }

            if (value is RedisString && long.TryParse(value.ToString(), out var x))
            {
                return x;
            }

            return 0;
        }

        public static double ToDouble(this IRedisObject value)
        {
            if (value is RedisInteger i)
            {
                return i.Value;
            }

            if (value is RedisString && double.TryParse(value.ToString(), out var x))
            {
                return x;
            }

            return 0;
        }

        public static RedisKey ToKey(this IRedisObject value)
            => value.ToString();
    }
}