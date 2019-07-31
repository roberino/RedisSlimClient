﻿using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Scheduling;
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

        public void RegisterHandler(Func<ReadOnlySequence<byte>, SequencePosition?> delimiter, Action<ReadOnlySequence<byte>> handler)
        {
            _delimiter = delimiter;
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

        bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        async Task PumpFromSocket()
        {
            var writer = _pipe.Writer;

            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    await _socket.AwaitAvailableSocket(_cancellationToken);

                    var memory = writer.GetMemory(_minBufferSize);

                    StateChanged?.Invoke(PipelineStatus.ReceivingFromSocket);

                    var bytesRead = await _socket.ReceiveAsync(memory);

                    if (IsRunning)
                    {
                        StateChanged?.Invoke(PipelineStatus.Advancing);

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
                    await _socket.AwaitAvailableSocket(_cancellationToken);

                    var result = await _pipe.Reader.ReadAsync(_cancellationToken);

                    var buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = Delimit(buffer);

                        if (position.HasValue)
                        {
                            // Odd behaviour - the Slice() function takes the end to be exclusive
                            var posIncDelimitter = buffer.GetPosition(1, position.Value);

                            var next = buffer.Slice(0, posIncDelimitter);

                            buffer = buffer.Slice(posIncDelimitter);

                            Handle(next);
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

            _pipe.Reader.Complete();

            if (error != null)
                Error?.Invoke(error);
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