using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Types.Primatives
{
    interface IMemoryCursor
    {
        Task Write(byte data);
        Task Write(byte[] data);
    }
}