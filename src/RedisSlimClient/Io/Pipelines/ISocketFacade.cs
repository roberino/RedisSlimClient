using System;
using System.Buffers;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface ISocket : IDisposable
    {
        SocketState State { get; }
        Task ConnectAsync();
        Task<int> ReceiveAsync(Memory<byte> memory);
        Task<int> SendAsync(ReadOnlySequence<byte> buffer);
    }
}