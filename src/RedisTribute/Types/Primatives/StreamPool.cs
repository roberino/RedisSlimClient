using System;
using System.Buffers;
using System.Threading;

namespace RedisTribute.Types.Primatives
{
    sealed class StreamPool
    {
        readonly ArrayPool<byte> _pool;
        readonly Action<byte[]> _releaser;
        long _bytesRented;

        StreamPool()
        {
            _pool = ArrayPool<byte>.Create();
            _releaser = Release;
            Null = new PooledStream(_ => { }, true, new byte[0], 0);
        }

        public static StreamPool Instance { get; } = new StreamPool();

        public PooledStream Null { get; }

        public long PooledMemory => _bytesRented;

        public PooledStream CreateWritable(int size)
        {
            var arr = Rent(size);

            return new PooledStream(_releaser, false, arr, size);
        }

        public PooledStream CreateReadonly(byte[] data)
        {
            return new PooledStream(_ => { }, true, data, data.Length);
        }

        public PooledStream CreateReadOnlyCopy(ReadOnlySequence<byte> data)
        {
            var arr = Rent((int)data.Length);

            if (data.IsSingleSegment)
            {
                var mem = new Memory<byte>(arr);
                data.First.CopyTo(mem);
            }
            else
            {
                var pos = 0;

                foreach (var span in data)
                {
                    var mem = new Memory<byte>(arr, pos, span.Length);

                    span.CopyTo(mem);

                    pos += span.Length;
                }
            }

            return new PooledStream(_releaser, true, arr, (int)data.Length);
        }

        public void Dispose()
        {
        }

        byte[] Rent(int minSize)
        {
            var arr = _pool.Rent(minSize);

            Interlocked.Add(ref _bytesRented, arr.Length);

            return arr;
        }

        void Release(byte[] arr)
        {
            var len = arr.Length;

            _pool.Return(arr);

            Interlocked.Add(ref _bytesRented, -len);
        }
    }
}