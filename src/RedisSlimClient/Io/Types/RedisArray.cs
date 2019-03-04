using System.Collections;
using System.Collections.Generic;

namespace RedisSlimClient.Io.Types
{
    class RedisArray : RedisObject, IReadOnlyCollection<RedisObject>
    {
        public RedisArray(long length) : base(RedisType.Array)
        {
            Count = (int)length;
            Items = new List<RedisObject>();
        }

        public bool IsComplete => Items.Count == Count;

        public int Count { get; }

        public IEnumerator<RedisObject> GetEnumerator() => Items.GetEnumerator();

        internal IList<RedisObject> Items { get; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}