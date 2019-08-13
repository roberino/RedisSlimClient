using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Scheduling;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineReceiver : IPipelineReceiver, ISchedulable
    {
        readonly int _minBufferSize;
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;

        volatile bool _reset;
        Func<ReadOnlySequence<byte>, SequencePosition?> _delimiter;
        Action<ReadOnlySequence<byte>> _handler;

        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, int minBufferSize = 1024)
        {
            _cancellationToken = cancellationToken;
            _minBufferSize = minBufferSize;
            _socket = socket;

            _pipe = new Pipe();
        }

        public Uri EndpointIdentifier => _socket.EndpointIdentifier;
        public event Action<Exception> Error;
        public event Action<PipelineStatus> StateChanged;
        public event Action<(string Action, byte[] Data)> Trace;

        public void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimiter, Action<ReadOnlySequence<byte>> handler)
        {
            _delimiter = delimiter;
            _handler = handler;
        }

        public void Schedule(IWorkScheduler scheduler)
        {
            scheduler.Schedule(RunAsync);
        }

        public async Task RunAsync()
        {
            _reset = false;

            var readerTask = PumpFromSocket();
            var pubTask = ReadPipeAsync();

            await Task.WhenAll(readerTask, pubTask);

            if (_reset)
            {
                _pipe.Reader.CancelPendingRead();
                _pipe.Writer.CancelPendingFlush();
                _pipe.Reset();
            }
        }

        public Task Reset()
        {
            _reset = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Error = null;
            StateChanged = null;

            _pipe.Reader.Complete();
            _pipe.Writer.Complete();

            try
            {
                _pipe.Reset();
            }
            catch { }
        }

        public bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        async Task PumpFromSocket()
        {
            var writer = _pipe.Writer;

            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    StateChanged?.Invoke(PipelineStatus.AwaitingConnection);

                    await _socket.AwaitAvailableSocket(_cancellationToken).ConfigureAwait(false);

                    var memory = writer.GetMemory(_minBufferSize);

                    StateChanged?.Invoke(PipelineStatus.ReceivingFromSocket);

                    var bytesRead = await _socket.ReceiveAsync(memory).ConfigureAwait(false);

                    if (IsRunning)
                    {
                        StateChanged?.Invoke(PipelineStatus.AdvancingWriter);

                        writer.Advance(bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;

                    break;
                }

                StateChanged?.Invoke(PipelineStatus.Flushing);

                var result = await writer.FlushAsync(_cancellationToken).ConfigureAwait(false);

                StateChanged?.Invoke(PipelineStatus.Flushed);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete(error);

            if (error != null)
            {
                StateChanged?.Invoke(PipelineStatus.Faulted);
                Error?.Invoke(error);
            }
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
                    StateChanged?.Invoke(PipelineStatus.ReadingFromPipe);

                    var result = await _pipe.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);

                    StateChanged?.Invoke(PipelineStatus.ReadFromPipe);

                    var buffer = result.Buffer;

                    SequencePosition? position;

                    do
                    {
                        StateChanged?.Invoke(PipelineStatus.Delimiting);

                        position = Delimit(buffer);

                        if (position.HasValue)
                        {
                            StateChanged?.Invoke(PipelineStatus.ProcessingData);

                            // Odd behaviour - the Slice() function takes the end to be exclusive
                            var posIncDelimiter = buffer.GetPosition(1, position.Value);

                            var next = buffer.Slice(0, posIncDelimiter);

                            buffer = buffer.Slice(posIncDelimiter);

                            Handle(next);
                        }
                        else
                        {
                            StateChanged?.Invoke(PipelineStatus.ReadingMoreData);
                            Trace?.Invoke((PipelineStatus.ReadingMoreData.ToString(), buffer.ToArray()));
                        }
                    }
                    while (position.HasValue && !buffer.IsEmpty);

                    if (_reset)
                    {
                        break;
                    }

                    StateChanged?.Invoke(PipelineStatus.AdvancingReader);

                    _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    error = ex;
                    break;
                }
            }

            _pipe.Reader.Complete(error);

            if (error != null)
            {
                StateChanged?.Invoke(PipelineStatus.Faulted);
                Error?.Invoke(error);
            }
        }

        SequencePosition? Delimit(ReadOnlySequence<byte> buffer)
        {
            try
            {
                return _delimiter.Invoke(buffer);
            }
            catch (Exception ex)
            {
                throw new BufferReadException(buffer, ex);
            }
        }

        void Handle(ReadOnlySequence<byte> buffer)
        {
            try
            {
                _handler.Invoke(buffer);
            }
            catch (Exception ex)
            {
                throw new BufferReadException(buffer, ex);
            }
        }
    }
}