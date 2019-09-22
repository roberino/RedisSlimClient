using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace RedisTribute.Types.Primatives
{
    class PooledStream : Stream
    {
        readonly MemoryStream _internalStream;
        readonly Action _onDispose;
        readonly byte[] _buffer;
        readonly int _maxSize;

        int _actualLength;

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
            base.Dispose(disposing);
            if (disposing)
                _onDispose();
        }

        public override string ToString() => DefaultEncoding.GetString(_buffer, 0, (int)Length);

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
    }

    sealed class StreamPool
    {
        readonly ArrayPool<byte> _pool;

        StreamPool()
        {
            _pool = ArrayPool<byte>.Create();
        }

        public static StreamPool Instance { get; } = new StreamPool();

        public PooledStream GetStream(int size)
        {
            var arr = _pool.Rent(size);

            return new PooledStream(() => _pool.Return(arr), false, arr, size);
        }
        public PooledStream CopyFrom(ReadOnlySequence<byte> data)
        {
            var arr = _pool.Rent((int)data.Length);

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

            return new PooledStream(() => _pool.Return(arr), true, arr, (int)data.Length);
        }

        public void Dispose()
        {
        }
    }
}
