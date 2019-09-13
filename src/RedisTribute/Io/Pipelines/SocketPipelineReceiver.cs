using RedisTribute.Io.Net;
using RedisTribute.Io.Scheduling;
using RedisTribute.Util;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    class SocketPipelineReceiver : IPipelineReceiver, ISchedulable, IResetable
    {
        readonly int _minBufferSize;
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;
        readonly AsyncLock _resetLock;

        volatile bool _resetting;
        Func<ReadOnlySequence<byte>, SequencePosition?> _delimiter;
        Action<ReadOnlySequence<byte>> _handler;

        public SocketPipelineReceiver(ISocket socket, CancellationToken cancellationToken, int minBufferSize = 1024)
        {
            _cancellationToken = cancellationToken;
            _minBufferSize = minBufferSize;
            _socket = socket;

            _resetLock = new AsyncLock();
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
            _resetting = false;

            var readerTask = PumpFromSocket();
            var pubTask = ReadPipeAsync();

            await Task.WhenAll(readerTask, pubTask);
        }

        public async Task<IDisposable> ResetAsync()
        {
            _resetting = true;

            StateChanged?.Invoke(PipelineStatus.Resetting);

            var handle = await _resetLock.LockAsync();

            _pipe.Reader.CancelPendingRead();
            _pipe.Writer.CancelPendingFlush();
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
            _pipe.Reset();

            _resetting = false;

            return handle;
        }

        public void Dispose()
        {
            Error = null;
            StateChanged = null;

            _resetLock.Dispose();

            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
        }

        public bool IsRunning => (!_cancellationToken.IsCancellationRequested);

        async Task PumpFromSocket()
        {
            var writer = _pipe.Writer;

            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    await AwaitReset();

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
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    error = ex;

                    StateChanged?.Invoke(PipelineStatus.Faulted);
                    Error?.Invoke(ex);

                    continue;
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
        }

        async Task AwaitReset()
        {
            while (_resetting)
            {
                StateChanged?.Invoke(PipelineStatus.AwaitingReset);
                await _resetLock.AwaitAsync();
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
                    await AwaitReset();

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
                    StateChanged?.Invoke(PipelineStatus.Faulted);
                    Error?.Invoke(ex);
                }
            }

            _pipe.Reader.Complete(error);
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