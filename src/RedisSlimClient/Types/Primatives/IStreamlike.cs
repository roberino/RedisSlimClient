using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Types.Primatives
{
    interface IStreamlike
    {
        Task WriteAsync(ArraySegment<byte> array);
    }
}
