using RedisSlimClient.Types.Primatives;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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

        public event Action<Exception> Error;

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

        public void Reset()
        {
            _reset = true;
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

            await _memoryCursor.FlushAsync();
        }

        async Task PumpToSocket()
        {
            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    var result = await _pipe.Reader.ReadAsync(_cancellationToken);

                    if (IsRunning)
                    {
                        var bytes = await _socket.SendAsync(result.Buffer);

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
                Error?.Invoke(error);
        }

        bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        public void Dispose()
        {
            Error = null;

            try
            {
                _pipe.Reader.Complete();
                _pipe.Writer.Complete();
            }
            catch { }
        }
    }
}