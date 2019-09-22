using System;
using System.Buffers;

namespace RedisTribute.Types.Primatives
{
    struct ArraySegmentByteSequenceAdapter : IByteSequence
    {
        readonly ArraySegment<byte> _segment;

        public ArraySegmentByteSequenceAdapter(ArraySegment<byte> segment)
        {
            _segment = segment;
        }

        public int Length => _segment.Count;

        public void CopyTo(byte[] array)
        {
            Array.Copy(_segment.Array, _segment.Offset, array, 0, Math.Min(array.Length, _segment.Count));
        }

        public byte[] ToArray(int offset)
        {
#if NET_CORE
            if (offset == 0)
                return _segment.ToArray();
#endif

            var buff = new byte[_segment.Count - offset];

            Array.Copy(_segment.Array, _segment.Offset + offset, buff, 0, buff.Length);
                        
            return buff;
        }

        public byte GetValue(int index) => _segment.Array[index + _segment.Offset];

        public ReadOnlySequence<byte> ToSequence(int offset)
        {
            if (offset == 0)
                return new ReadOnlySequence<byte>(_segment.AsMemory());

            return new ReadOnlySequence<byte>(_segment.Array, _segment.Offset + offset, _segment.Count - offset);
        }
    }
}