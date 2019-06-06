using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineSender : IPipelineSender, IRunnable
    {
        readonly int _minBufferSize;
        readonly ISocket _socket;
        readonly Pipe _pipe;

        CancellationToken _cancellationToken;

        public SocketPipelineSender(ISocket socket, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _socket = socket;

            _pipe = new Pipe();
        }

        public event Action<Exception> OnException;

        public Task RunAsync() => PumpToSocket();

        public async Task SendAsync(byte[] data)
        {
            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data));
        }

        async Task PumpToSocket()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _pipe.Reader.ReadAsync(_cancellationToken);

                    var bytes = await _socket.SendAsync(result.Buffer);

                    _pipe.Reader.AdvanceTo(result.Buffer.End);
                }
                catch(Exception ex)
                {
                    OnException?.Invoke(ex);
                    _pipe.Reader.Complete();
                    break;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}