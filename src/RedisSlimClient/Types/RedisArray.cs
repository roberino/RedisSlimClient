using System.Collections.Generic;

namespace RedisSlimClient.Types
{
    internal class RedisArray : List<IRedisObject>, IRedisObject, IReadOnlyCollection<IRedisObject>
    {
        readonly int _count;

        public RedisArray(long length)
        {
            _count = (int)length;
        }

        public RedisArray(params IRedisObject[] items)
        {
            _count = items.Length;
            AddRange(items);
        }

        public new int Count => _count;

        public bool IsComplete => _count == base.Count;

        public bool IsNull => false;

        public RedisType Type => RedisType.Array;
    }
}