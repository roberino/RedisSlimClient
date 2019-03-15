using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RedisSlimClient.Io
{
    internal class StreamIterator : IEnumerable<ArraySegment<byte>>, IDisposable
    {
        readonly Stream _stream;
        readonly byte[] _buffer;

        readonly MemoryStream _overflow;
        readonly CancellationTokenSource _cancellationToken;

        bool _startEndFlag;

        public StreamIterator(Stream stream, int bufferSize = 1024)
        {
            _stream = stream;

            _overflow = new MemoryStream();
            _buffer = new byte[bufferSize];
            _cancellationToken = new CancellationTokenSource();
        }

        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var read = _stream.Read(_buffer, 0, _buffer.Length);

                if (read == 0)
                {
                    yield break;
                }

                DebugOutput.Dump(_buffer, read);

                foreach (var segment in Iterate(_buffer, 0, read))
                {
                    yield return segment;
                }
            }
        }

        IEnumerable<ArraySegment<byte>> Iterate(byte[] buffer, int offset, int length)
        {
            var i = offset;
            var currentOffset = offset;

            while (i < length)
            {
                var c = buffer[i];

                if (c == '\r')
                {
                    _startEndFlag = true;
                }
                else
                {
                    if (c == '\n' && _startEndFlag)
                    {
                        yield return GetNextSegment(currentOffset, i - currentOffset - 1);
                        currentOffset = i + 1;
                    }

                    _startEndFlag = false;
                }

                i++;
            }

            if (currentOffset < length)
            {
                _overflow.Write(buffer, currentOffset, length - currentOffset);
            }
        }

        ArraySegment<byte> GetNextSegment(int offset, int count)
        {
            var newBuffer = _buffer;

            if (_overflow.Position > 0)
            {
                if (count > 0)
                {
                    _overflow.Write(_buffer, offset, count);
                    count = (int)_overflow.Position;
                }
                else
                {
                    count = (int)_overflow.Position + count;
                }

                newBuffer = _overflow.ToArray();

                offset = 0;

                _overflow.Position = 0;
            }

            return new ArraySegment<byte>(newBuffer, offset, count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();

            _overflow?.Dispose();
        }
    }
}