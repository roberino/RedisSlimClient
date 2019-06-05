using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineReceiver : IPipelineReceiver
    {
        readonly byte _delimitter;
        readonly int _minBufferSize;
        readonly Socket _socket;
        readonly Pipe _pipe;
        readonly AwaitableSocketAsyncEventArgs _socketEventArgs;
        readonly CancellationToken _cancellationToken;

        public SocketPipelineReceiver(Socket socket, CancellationToken cancellationToken, byte delimitter, int minBufferSize = 512)
        {
            _cancellationToken = cancellationToken;
            _delimitter = delimitter;
            _minBufferSize = minBufferSize;
            _socket = socket;

            _pipe = new Pipe();
            _socketEventArgs = new AwaitableSocketAsyncEventArgs(new Memory<byte>());

            _cancellationToken.Register(_socketEventArgs.Abandon);
        }

        public event Action<Exception> OnException;

        public event Action<ReadOnlySequence<byte>> OnRead;

        public Task RunAsync()
        {
            return Task.WhenAll(PumpFromSocket(), ReadPipeAsync());
        }
        public void Dispose()
        {
        }

        async Task PumpFromSocket()
        {
            var writer = _pipe.Writer;

            while (!_cancellationToken.IsCancellationRequested)
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
            while (!_cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await _pipe.Reader.ReadAsync(_cancellationToken);

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