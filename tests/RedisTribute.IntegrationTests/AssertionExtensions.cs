using System.Collections.Generic;

namespace RedisTribute.IntegrationTests
{
    static class AssertionExtensions
    {
        public static IList<T> SequentialDedupe<T>(this IList<T> items)
        {
            var results = new List<T>();

            var last = default(T);

            foreach (var result in items)
            {
                if (results.Count == 0 || !result.Equals(last))
                {
                    results.Add(result);

                    last = result;
                }
            }

            return results;
        }
    }
}
