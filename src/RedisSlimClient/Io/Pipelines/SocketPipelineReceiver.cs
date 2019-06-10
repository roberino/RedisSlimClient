using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineReceiver : IPipelineReceiver, IRunnable
    {
        readonly Func<ReadOnlySequence<byte>, SequencePosition?> _delimitter;
        readonly int _minBufferSize;
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;

        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, byte delimitter, int minBufferSize = 512)
            : this(socket, cancellationToken, s => s.PositionOf(delimitter), minBufferSize)
        {
        }
        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, int minBufferSize = 512)
        {
            _cancellationToken = cancellationToken;
            _delimitter = delimitter;
            _minBufferSize = minBufferSize;
            _socket = socket;

            _pipe = new Pipe();
        }

        public event Action<Exception> Error;

        public event Action<ReadOnlySequence<byte>> Reading;

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

                try
                {
                    var bytesRead = await _socket.ReceiveAsync(memory);

                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);

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
                    position = _delimitter(buffer);
                    
                    if (position.HasValue)
                    {
                        var next = buffer.Slice(0, position.Value);

                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

                        Reading?.Invoke(next);
                    }
                }
                while (position.HasValue);

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