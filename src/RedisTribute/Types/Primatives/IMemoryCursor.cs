using System;
using System.Threading.Tasks;

namespace RedisTribute.Types.Primatives
{
    interface IMemoryCursor
    {
        int CurrentPosition { get; }

        ValueTask Write(byte data);
        ValueTask Write(byte[] data);
        ValueTask Write(ArraySegment<byte> data);
    }
}