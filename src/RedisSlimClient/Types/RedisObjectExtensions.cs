using System.Collections.Generic;

namespace RedisSlimClient.Types
{
    static class RedisObjectExtensions
    {
        public static IEnumerable<RedisObject> ToObjects(this IEnumerable<RedisObjectPart> parts)
        {
            RedisArray currentArray = null;

            foreach (var part in parts)
            {
                if (part.ArrayIndex.HasValue)
                {
                    if (currentArray == null)
                    {
                        currentArray = new RedisArray(part.Length);
                    }
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
    }
}