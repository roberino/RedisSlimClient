using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineSender : IPipelineSender
    {
        readonly int _minBufferSize;
        readonly Socket _socket;
        readonly Pipe _pipe;
        readonly AwaitableSocketAsyncEventArgs _socketEventArgs;

        CancellationToken _cancellationToken;

        public SocketPipelineSender(Socket socket, CancellationToken cancellationToken, byte delimitter, int minBufferSize = 512)
        {
            _minBufferSize = minBufferSize;
            _cancellationToken = cancellationToken;
            _socket = socket;

            _pipe = new Pipe();
            _socketEventArgs = new AwaitableSocketAsyncEventArgs(new Memory<byte>());
        }

        public event Action<Exception> OnException;

        public void Dispose()
        {
            _socketEventArgs.Abandon();
        }

        public async Task SendAsync(byte[] data)
        {
            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data));
        }

        async Task PumpToSocket()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                var result = await _pipe.Reader.ReadAsync(_cancellationToken);

                await SendToSocket(result.Buffer);

                _pipe.Reader.AdvanceTo(result.Buffer.End);
            }
        }

        async Task SendToSocket(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            if (buffer.IsSingleSegment)
            {
                await SendToSocket(buffer.First);
                return;
            }

            foreach (var item in buffer)
            {
                await SendToSocket(item);
            }
        }

        async Task SendToSocket(ReadOnlyMemory<byte> buffer)
        {
            _socketEventArgs.Reset(buffer);

            if (!_socket.SendAsync(_socketEventArgs))
            {
                _socketEventArgs.Complete();
            }

            var bytesWritten = await _socketEventArgs;
        }
    }
}