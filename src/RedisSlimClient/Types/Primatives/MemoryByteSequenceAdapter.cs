using System;

namespace RedisSlimClient.Types.Primatives
{
    struct MemoryByteSequenceAdapter : IByteSequence
    {
        readonly Memory<byte> _memory;

        public MemoryByteSequenceAdapter(Memory<byte> memory)
        {
            _memory = memory;
        }
        public int Length => _memory.Length;

        public void CopyTo(byte[] array)
        {
            _memory.CopyTo(new Memory<byte>(array));
        }

        public byte[] ToArray(int offset) => 
            _memory.Slice(offset).ToArray();

        public byte GetValue(int index) =>
            _memory.Slice(index, 1).ToArray()[0];
    }
}
