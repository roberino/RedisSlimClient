﻿using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    interface ISocket : IDisposable
    {
        event Action<ReceiveStatus> Receiving;
        Uri EndpointIdentifier { get; }
        SocketState State { get; }
        Task ConnectAsync();
        Task AwaitAvailableSocket(CancellationToken cancellation);
        ValueTask<int> ReceiveAsync(Memory<byte> memory);
        ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer);
    }
}