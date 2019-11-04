using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;

namespace RedisTribute.Types.Primatives
{
    class PooledStream : Stream
    {
        readonly MemoryStream _internalStream;
        readonly Action _onDispose;
        readonly byte[] _buffer;
        readonly int _maxSize;

        int _actualLength;

        bool _disposed = false;

        public PooledStream(Action onDispose, bool readOnly, byte[] buffer, int size)
        {
            _onDispose = onDispose;
            _buffer = buffer;
            _maxSize = size;
            _internalStream = new MemoryStream(buffer, 0, size);
            _actualLength = readOnly ? size : 0;
            CanWrite = !readOnly;
        }

        internal Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

        public byte[] ToArray()
        {
            var bytes = new byte[Length];

            Array.Copy(_buffer, bytes, bytes.Length);

            return bytes;
        }

        public ArraySegment<byte> GetBuffer()
        {
            _internalStream.Flush();

            return new ArraySegment<byte>(_buffer, 0, _actualLength);
        }

        public override long Length => _actualLength;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite { get; }
        public override long Position
        {
            get => _internalStream.Position;
            set => _internalStream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                try
                {
                    _internalStream.Dispose();
                }
                catch { }
                try
                {
                    _onDispose();
                }
                catch { }
            }

            base.Dispose(disposing);
        }

        public override string ToString() => ToString(DefaultEncoding);

        public string ToString(Encoding encoding) => encoding.GetString(_buffer, 0, (int)Length);

        public override void Flush()
        {
            _internalStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) => _internalStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _internalStream.Seek(offset, origin);

        public override void SetLength(long value)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }

            if (value > _maxSize)
            {
                throw new NotSupportedException();
            }

            _internalStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
            {
                throw new NotSupportedException();
            }
            _internalStream.Write(buffer, offset, count);
            _actualLength += count;
        }
        ~PooledStream()
        {
            Dispose();
        }
    }

    sealed class StreamPool
    {
        readonly ArrayPool<byte> _pool;

        long _bytesRented;

        StreamPool()
        {
            _pool = ArrayPool<byte>.Create();
        }

        public static StreamPool Instance { get; } = new StreamPool();

        public long PooledMemory => _bytesRented;

        public PooledStream CreateWritable(int size)
        {
            var arr = Rent(size);

            return new PooledStream(() => Release(arr), false, arr, size);
        }

        public PooledStream CreateReadonly(byte[] data)
        {
            return new PooledStream(() => { }, true, data, data.Length);
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

            return new PooledStream(() => Release(arr), true, arr, (int)data.Length);
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