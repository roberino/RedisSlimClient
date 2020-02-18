using RedisTribute.Io.Net;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.UnitTests.Io.Pipelines
{
    class StubSocket : ISocket
    {
        readonly ManualResetEvent _sendWaitHandle;
        readonly ManualResetEvent _receiveWaitHandle;
        readonly ManualResetEvent _connectionWaitHandle;

        Exception _reconnectError;

        public event Action<ReceiveStatus> Receiving;

        public StubSocket()
        {
            _sendWaitHandle = new ManualResetEvent(false);
            _receiveWaitHandle = new ManualResetEvent(false);
            _connectionWaitHandle = new ManualResetEvent(false);
            Received = new ConcurrentQueue<byte[]>();
            State = new SocketState(() => true);
        }

        public void RaiseError(Exception ex = null)
        {
            _connectionWaitHandle.Reset();
            State.WriteError(ex ?? new TimeoutException());
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

                _connectionWaitHandle.Set();

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
            _connectionWaitHandle.Dispose();
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
            var bytes = ReadReceivedQueue(memory);

            await Task.Delay(1);

            _receiveWaitHandle.Set();

            return bytes;
        }

        public async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            Received.Enqueue(buffer.ToArray());

            await Task.Delay(1);

            _sendWaitHandle.Set();

            return (int)buffer.Length;
        }

        public async Task SendStringAsync(string data)
        {
            await SendAsync(new ReadOnlySequence<byte>(data.Select(c => (byte)c).ToArray()));
        }

        public async Task<byte[]> WaitForData(int length)
        {
            var i = 0;

            while (i++ < 10)
            {
                if (Received.Sum(x => x.Length) >= length)
                {
                    return Received.SelectMany(x => x).ToArray();
                }

                await Task.Delay(10);
            }

            throw new Exception("Data not received");
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

        public void WaitForConnect()
        {
            _connectionWaitHandle.WaitOne(5000);
        }

        public ConcurrentQueue<byte[]> Received { get; }

        public SocketState State { get; }

        public Uri EndpointIdentifier => new Uri("master://localhost:8679");

        int ReadReceivedQueue(Memory<byte> memory)
        {
            var i = 0;
            while (!Received.IsEmpty)
            {
                if (Received.TryDequeue(out var next))
                {
                    foreach (var b in next)
                    {
                        memory.Span[i++] = b;
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
