using System;
using System.Buffers;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Net
{
    interface ISocket : IDisposable
    {
        SocketState State { get; }
        Task ConnectAsync();
        ValueTask<int> ReceiveAsync(Memory<byte> memory);
        ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer);
    }
}