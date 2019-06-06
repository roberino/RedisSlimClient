using System;
using System.Buffers;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface ISocket : IDisposable
    {
        Task<int> ReceiveAsync(Memory<byte> memory);
        Task<int> SendAsync(ReadOnlySequence<byte> buffer);
    }
}