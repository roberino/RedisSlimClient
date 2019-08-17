using System.Collections.Generic;

namespace RedisSlimClient.Types
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

            return 0;
        }
    }
}