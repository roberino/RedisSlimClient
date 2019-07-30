using RedisSlimClient.Types.Primatives;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Io.Net;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineSender : IPipelineSender, IRunnable
    {
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;
        readonly MemoryCursor _memoryCursor;

        volatile bool _reset;

        public SocketPipelineSender(ISocket socket, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _socket = socket;

            _pipe = new Pipe();

            _memoryCursor = new MemoryCursor(_pipe.Writer);
        }

        public Uri EndpointIdentifier => _socket.EndpointIdentifier;
        public event Action<Exception> Error;
        public event Action<PipelineStatus> StateChanged;

        public async Task RunAsync()
        {
            _reset = false;

            await PumpToSocket();

            if (_reset)
            {
                _pipe.Reset();
                _reset = false;
            }
        }

        public Task Reset()
        {
            _reset = true;
            return Task.CompletedTask;
        }

        public async Task SendAsync(byte[] data)
        {
            if (_reset)
            {
                throw new InvalidOperationException("Resetting");
            }

            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data));
        }

        public async Task SendAsync(Func<IMemoryCursor, Task> writeAction)
        {
            if (_reset)
            {
                throw new InvalidOperationException("Resetting");
            }

            await writeAction(_memoryCursor);

            StateChanged?.Invoke(PipelineStatus.WritingToPipe);

            await _memoryCursor.FlushAsync();
        }

        async Task PumpToSocket()
        {
            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    StateChanged?.Invoke(PipelineStatus.ReadingFromPipe);

                    var result = await _pipe.Reader.ReadAsync(_cancellationToken);
                    
                    if (IsRunning)
                    {
                        StateChanged?.Invoke(PipelineStatus.SendingToSocket);

                        var bytes = await _socket.SendAsync(result.Buffer);

                        StateChanged?.Invoke(PipelineStatus.Advancing);

                        _pipe.Reader.AdvanceTo(result.Buffer.GetPosition(bytes));
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
            {
                StateChanged?.Invoke(PipelineStatus.Faulted);
                Error?.Invoke(error);
            }
        }

        bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        public void Dispose()
        {
            Error = null;

            try
            {
                _pipe.Reader.Complete();
                _pipe.Writer.Complete();
                _pipe.Reset();
            }
            catch { }
        }
    }
}