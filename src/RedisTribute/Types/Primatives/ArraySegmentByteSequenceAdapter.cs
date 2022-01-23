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
            if (_segment.Array != null)
                Array.Copy(_segment.Array, _segment.Offset, array, 0, Math.Min(array.Length, _segment.Count));
        }

        public byte[] ToArray(int offset)
        {
#if NET_CORE
            if (offset == 0)
                return _segment.ToArray();
#endif

            var buff = new byte[_segment.Count - offset];

            if (_segment.Array != null)
                Array.Copy(_segment.Array, _segment.Offset + offset, buff, 0, buff.Length);
                        
            return buff;
        }

        public byte GetValue(int index) => _segment.Array == null ? (byte)0 : _segment.Array[index + _segment.Offset];

        public ReadOnlySequence<byte> ToSequence(int offset)
        {
            if (offset == 0)
                return new ReadOnlySequence<byte>(_segment.AsMemory());

            if (_segment.Array == null)
                return new ReadOnlySequence<byte>();

            return new ReadOnlySequence<byte>(_segment.Array, _segment.Offset + offset, _segment.Count - offset);
        }
    }
}