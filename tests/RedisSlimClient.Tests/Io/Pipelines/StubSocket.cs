using RedisSlimClient.Io.Net;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    class StubSocket : ISocket
    {
        readonly ManualResetEvent _sendWaitHandle;
        readonly ManualResetEvent _receiveWaitHandle;

        Exception _reconnectError;

        public event Action<ReceiveStatus> Receiving;

        public StubSocket()
        {
            _sendWaitHandle = new ManualResetEvent(false);
            _receiveWaitHandle = new ManualResetEvent(false);
            Received = new ConcurrentQueue<ReadOnlySequence<byte>>();
            State = new SocketState(() => true);
        }

        public void RaiseError(Exception ex = null)
        {
            State.ReadError(ex ?? new TimeoutException());
        }

        public void BreakReconnection(Exception ex = null)
        {
            _reconnectError = ex ?? new TimeoutException();
        }

        public int CallsToConnect { get; private set; }

        public Task ConnectAsync()
        {
            CallsToConnect++;

            return State.DoConnect(() =>
            {
                if (_reconnectError != null)
                {
                    throw _reconnectError;
                }

                return Task.CompletedTask;
            });
        }

        public Task AwaitAvailableSocket(CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            State.Terminated();

            _sendWaitHandle.Dispose();
            _receiveWaitHandle.Dispose();
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
            var bytes = ReadReceivedQueue(memory);

            await Task.Delay(10);

            _receiveWaitHandle.Set();

            return bytes;
        }

        public async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
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

        public Uri EndpointIdentifier => new Uri("master://localhost:8679");

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

        public override string ToString()
        {
            return Received.Aggregate(new StringBuilder(), (s, x) => s.Append(Encoding.ASCII.GetString(x.ToArray()))).ToString();
        }
    }
}
