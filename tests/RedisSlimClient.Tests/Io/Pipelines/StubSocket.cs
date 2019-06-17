﻿using RedisSlimClient.Io.Pipelines;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    class StubSocket : ISocket
    {
        readonly ManualResetEvent _sendWaitHandle;
        readonly ManualResetEvent _receiveWaitHandle;

        public StubSocket()
        {
            _sendWaitHandle = new ManualResetEvent(false);
            _receiveWaitHandle = new ManualResetEvent(false);
            Received = new ConcurrentQueue<ReadOnlySequence<byte>>();
            State = new SocketState(() => true);
        }

        public Task ConnectAsync()
        {
            return State.DoConnect(() => Task.CompletedTask);
        }

        public void Dispose()
        {
            State.Terminated();

            _sendWaitHandle.Dispose();
            _receiveWaitHandle.Dispose();
        }

        public async Task<int> ReceiveAsync(Memory<byte> memory)
        {
            var bytes = ReadReceivedQueue(memory);

            await Task.Delay(10);

            _receiveWaitHandle.Set();

            return bytes;
        }

        public async Task<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            Received.Enqueue(buffer);

            await Task.Delay(10);

            _sendWaitHandle.Set();

            return (int)buffer.Length;
        }

        public async Task SendStringAsync(string data)
        {
            await SendAsync(new ReadOnlySequence<byte>(data.Select(c => (byte)c).ToArray()));
        }

        public void WaitForDataWrite()
        {
            _sendWaitHandle.WaitOne();
            _sendWaitHandle.Reset();
        }

        public void WaitForDataRead()
        {
            _receiveWaitHandle.WaitOne();
            _receiveWaitHandle.Reset();
        }

        public ConcurrentQueue<ReadOnlySequence<byte>> Received { get; }

        public SocketState State { get; }

        int ReadReceivedQueue(Memory<byte> memory)
        {
            var i = 0;

            while (!Received.IsEmpty)
            {
                if (Received.TryDequeue(out var next))
                {
                    foreach (var item in next)
                    {
                        foreach (var b in item.Span)
                        {
                            memory.Span[i++] = b;
                        }
                    }
                }
            }

            return i;
        }
    }
}