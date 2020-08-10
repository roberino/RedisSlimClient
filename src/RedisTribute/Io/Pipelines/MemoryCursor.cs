using RedisTribute.Types.Primatives;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    class MemoryCursor : IMemoryCursor
    {
        readonly PipeWriter _writer;
        readonly int _defaultBufferSize;

        Memory<byte> _memory;
        int _position;

        public MemoryCursor(PipeWriter writer, int defaultBufferSize = 512)
        {
            _writer = writer;
            _defaultBufferSize = defaultBufferSize;
        }

        public int CurrentPosition => _position;

        public async ValueTask Write(byte[] data)
        {
            var memTask = GetMemory(data.Length);

            if (!memTask.IsCompleted)
            {
                await memTask;
            }

            var mem = memTask.Result;

            for (var i = 0; i < data.Length; i++)
            {
                mem.Span[_position++] = data[i];
            }
        }

        public async ValueTask Write(ArraySegment<byte> data)
        {
            var memTask = GetMemory(data.Count);

            if (!memTask.IsCompleted)
            {
                await memTask;
            }

            var mem = memTask.Result;

            for (var i = data.Offset; i < data.Count; i++)
            {
                mem.Span[_position++] = data.Array[i];
            }
        }

        public async ValueTask Write(byte data)
        {
            var memTask = GetMemory(1);

            if (!memTask.IsCompleted)
            {
                await memTask;
            }

            var mem = memTask.Result;

            mem.Span[_position++] = data;
        }

        public async ValueTask<bool> FlushAsync()
        {
            if (_position > 0)
            {
                var result = await FlushAndReset();
                _memory = default;
                return result.IsCompleted;
            }
            return true;
        }

        async ValueTask<Memory<byte>> GetMemory(int length)
        {
            if (_memory.IsEmpty)
            {
                _position = 0;
                _memory = _writer.GetMemory(Math.Max(_defaultBufferSize, length));
            }
            else
            {
                if (_memory.Length - _position < length)
                {
                    await FlushAndReset();
                    _memory = _writer.GetMemory(Math.Max(_defaultBufferSize, length));
                }
            }

            return _memory;
        }

        ValueTask<FlushResult> FlushAndReset()
        {
            _writer.Advance(_position);
            _position = 0;
            return _writer.FlushAsync();
        }
    }
}