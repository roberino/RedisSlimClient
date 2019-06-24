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

        volatile bool _reset;

        public SocketPipelineSender(ISocket socket, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _socket = socket;

            _pipe = new Pipe();
        }

        public event Action<Exception> Error;

        public Task RunAsync() => PumpToSocket();

        public void Reset()
        {
            _reset = true;
            _pipe.Reset();
        }

        public async Task SendAsync(byte[] data)
        {
            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data));
        }

        public async Task SendAsync(Func<Memory<byte>, int> writeAction, int bufferSize = 512)
        {
            var mem = _pipe.Writer.GetMemory(bufferSize);

            var len = writeAction(mem);

            if (len > 0)
            {
                _pipe.Writer.Advance(len);

                await _pipe.Writer.FlushAsync(_cancellationToken);
            }
        }

        async Task PumpToSocket()
        {
            _reset = false;

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
                    Error?.Invoke(ex);
                    _pipe.Reader.Complete();
                    break;
                }
            }
        }

        bool IsRunning => (!_cancellationToken.IsCancellationRequested && !_reset);

        public void Dispose()
        {
            Error = null;

            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
        }
    }
}