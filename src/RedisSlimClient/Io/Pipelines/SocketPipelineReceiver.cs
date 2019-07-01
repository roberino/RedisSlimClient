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

        volatile bool _reset;
        Func<ReadOnlySequence<byte>, SequencePosition?> _delimitter;
        Action<ReadOnlySequence<byte>> _handler;

        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, int minBufferSize = 1024)
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

        public async Task RunAsync()
        {
            _reset = false;

            var readerTask = PumpFromSocket();
            var pubTask = ReadPipeAsync();

            await Task.WhenAll(readerTask, pubTask);

            if (_reset)
            {
                _pipe.Reset();
            }
        }

        public void Reset()
        {
            _reset = true;
        }

        public void Dispose()
        {
            Error = null;

            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
        }

        bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        async Task PumpFromSocket()
        {
            var writer = _pipe.Writer;

            Exception error = null;

            while (IsRunning)
            {
                var memory = writer.GetMemory(1);

                try
                {
                    var bytesRead = await _socket.ReceiveAsync(memory);

                    if (IsRunning)
                    {
                        writer.Advance(bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;

                    break;
                }

                var result = await writer.FlushAsync(_cancellationToken);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();

            if (error != null)
                Error?.Invoke(error);
        }

        async Task ReadPipeAsync()
        {
            if (_handler == null)
            {
                throw new InvalidOperationException("No registered handler");
            }

            Exception error = null;

            while (IsRunning)
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

                    if (_reset)
                    {
                        break;
                    }

                    _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    break;
                }
            }

            _pipe.Reader.Complete();

            if (error != null)
                Error?.Invoke(error);
        }
    }
}