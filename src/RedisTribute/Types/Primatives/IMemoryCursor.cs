using System;
using System.Threading.Tasks;

namespace RedisTribute.Types.Primatives
{
    interface IMemoryCursor
    {
        int CurrentPosition { get; }

        ValueTask<bool> Write(byte data);
        ValueTask<bool> Write(byte[] data);
        ValueTask<bool> Write(ArraySegment<byte> data);
    }
}