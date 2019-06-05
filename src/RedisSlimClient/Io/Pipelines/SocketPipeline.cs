using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipeline : ISocketPipeline
    {
        readonly EndPoint _endPoint;
        readonly byte _delimitter;
        readonly int _minBufferSize;
        readonly Socket _socket;
        readonly Pipe _pipe;
        readonly AwaitableSocketAsyncEventArgs _socketEventArgs;

        bool _disposed;

        public SocketPipeline(EndPoint endPoint, TimeSpan timeout, byte delimitter, int minBufferSize = 512)
        {
            _endPoint = endPoint;
            _delimitter = delimitter;
            _minBufferSize = minBufferSize;
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)timeout.TotalMilliseconds,
                SendTimeout = (int)timeout.TotalMilliseconds,
                NoDelay = true
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _pipe = new Pipe();
            _socketEventArgs = new AwaitableSocketAsyncEventArgs(new Memory<byte>());
        }

        public event Action<Exception> OnException;

        public event Action<ReadOnlySequence<byte>> OnRead;

        public Task RunAsync()
        {
            return Task.WhenAll(FillPipeAsync(), ReadPipeAsync());
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _socketEventArgs.Abandon();

                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }

                _socket.Dispose();
            }
        }

        ~SocketPipeline() { Dispose(); }

        async Task FillPipeAsync()
        {
            var writer = _pipe.Writer;

            while (!_disposed)
            {
                var memory = writer.GetMemory(_minBufferSize);

                _socketEventArgs.Reset(memory);

                try
                {

                    if (!_socket.ReceiveAsync(_socketEventArgs))
                    {
                        _socketEventArgs.Complete();
                    }

                    var bytesRead = await _socketEventArgs;

                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    _socketEventArgs.Abandon();

                    OnException?.Invoke(ex);

                    break;
                }

                var result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }

        async Task ReadPipeAsync()
        {
            while (!_disposed)
            {
                ReadResult result = await _pipe.Reader.ReadAsync();

                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = null;

                do
                {
                    position = buffer.PositionOf(_delimitter);

                    if (position != null)
                    {
                        var next = buffer.Slice(0, position.Value);

                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

                        OnRead?.Invoke(buffer);
                    }
                }
                while (position != null);

                _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            _pipe.Reader.Complete();
        }
    }
}