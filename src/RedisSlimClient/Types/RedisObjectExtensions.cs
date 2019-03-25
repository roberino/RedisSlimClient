using System.Collections.Generic;

namespace RedisSlimClient.Types
{
    static class RedisObjectExtensions
    {
        public static IEnumerable<RedisObject> ToObjects(this IEnumerable<RedisObjectPart> parts)
        {
            // e.g.
            // array:start:L1
            // string:value:L1
            // string:value:L1
            //      array:start:L2
            //      int:value:L2
            // string:value:0

            var objectStack = new Stack<RedisArray>();
            RedisArray currentArray = null;

            foreach (var part in parts)
            {
                while (part.Level < objectStack.Count)
                {
                    currentArray = objectStack.Pop();
                }

                if (part.IsArrayStart)
                {
                    var prev = currentArray;

                    currentArray = new RedisArray(part.Length);
                    objectStack.Push(currentArray);

                    prev?.Items.Add(currentArray);

                    continue;
                }

                if (part.IsArrayPart)
                {
                    currentArray.Items.Add(part.Value);

                    continue;
                }

                if (currentArray != null)
                {
                    yield return currentArray;
                    currentArray = null;
                }

                yield return part.Value;
            }

            if (currentArray != null)
            {
                yield return currentArray;
            }
        }

        public static long ToLong(this RedisObject value)
        {
            if (value is RedisInteger i)
            {
                return i.Value;
            }

            return 0;
        }
    }
}