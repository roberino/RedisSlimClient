using System.Threading;

namespace RedisSlimClient.Util
{
    class SyncedCounter
    {
        long _value;

        public long Value => Interlocked.Read(ref _value);

        public long Increment() => Interlocked.Increment(ref _value);

        public long Decrement() => Interlocked.Decrement(ref _value);
    }
}