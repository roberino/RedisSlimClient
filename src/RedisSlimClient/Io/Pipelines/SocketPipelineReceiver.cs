using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineReceiver : IPipelineReceiver, IRunnable
    {
        readonly int _minBufferSize;
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;

        Func<ReadOnlySequence<byte>, SequencePosition?> _delimitter;
        Action<ReadOnlySequence<byte>> _handler;

        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, int minBufferSize = 512)
        {
            _cancellationToken = cancellationToken;
            _minBufferSize = minBufferSize;
            _socket = socket;

            _pipe = new Pipe();
        }

        public event Action<Exception> Error;

        public void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, Action<ReadOnlySequence<byte>> handler)
        {
            _delimitter = delimitter;
            _handler = handler;
        }

        public Task RunAsync()
        {
            var readerTask = PumpFromSocket();
            var pubTask = ReadPipeAsync();

            return Task.WhenAll(readerTask, pubTask);
        }

        public void Dispose()
        {
            Error = null;
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
            if (_handler == null)
            {
                throw new InvalidOperationException("No registered handler");
            }

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _pipe.Reader.ReadAsync(_cancellationToken);

                    var buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = _delimitter(buffer);

                        if (position.HasValue)
                        {
                            // Odd behaviour - the Slice() function takes the end to be exclusive
                            var posIncDelimitter = buffer.GetPosition(1, position.Value);

                            var next = buffer.Slice(0, posIncDelimitter);

                            buffer = buffer.Slice(posIncDelimitter);

                            _handler.Invoke(next);
                        }
                    }
                    while (position.HasValue);

                    _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Error?.Invoke(ex);
                    throw;
                }
            }

            _pipe.Reader.Complete();
        }
    }
}