using System;
using System.Threading.Tasks;

namespace RedisTribute.Types.Primatives
{
    interface IStreamlike
    {
        Task WriteAsync(ArraySegment<byte> array);
    }
}
