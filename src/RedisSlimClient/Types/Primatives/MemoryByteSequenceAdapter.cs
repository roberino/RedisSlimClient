using System;
using System.Buffers;

namespace RedisSlimClient.Types.Primatives
{
    struct MemoryByteSequenceAdapter : IByteSequence
    {
        readonly ReadOnlySequence<byte> _sequence;

        public MemoryByteSequenceAdapter(ReadOnlySequence<byte> sequence)
        {
            _sequence = sequence;
        }

        public int Length => (int)_sequence.Length;

        public void CopyTo(byte[] array)
        {
            _sequence.CopyTo(new Span<byte>(array));
        }

        public byte[] ToArray(int offset) => 
            _sequence.Slice(offset).ToArray();

        public byte GetValue(int index) =>
            _sequence.Slice(index, 1).ToArray()[0];
    }
}
