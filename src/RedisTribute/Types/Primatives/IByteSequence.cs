using System.Buffers;

namespace RedisTribute.Types.Primatives
{
    interface IByteSequence
    {
        int Length { get; }
        byte[] ToArray(int offset = 0);
        void CopyTo(byte[] array);
        byte GetValue(int index);
        ReadOnlySequence<byte> ToSequence(int offset);
    }
}
