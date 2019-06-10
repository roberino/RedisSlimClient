using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketFacade : ISocket
    {
        readonly Socket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly AwaitableSocketAsyncEventArgs _readEventArgs;
        readonly AwaitableSocketAsyncEventArgs _writeEventArgs;

        public SocketFacade(EndPoint endPoint, TimeSpan timeout)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)timeout.TotalMilliseconds,
                SendTimeout = (int)timeout.TotalMilliseconds,
                NoDelay = true
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _readEventArgs = new AwaitableSocketAsyncEventArgs(new Memory<byte>());
            _writeEventArgs = new AwaitableSocketAsyncEventArgs(new Memory<byte>());
        }

        public async Task<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            var sent = 0;

            if (buffer.IsEmpty)
            {
                return sent;
            }

            if (buffer.IsSingleSegment)
            {
                await SendToSocket(buffer.First);
                return sent;
            }

            foreach (var item in buffer)
            {
                sent += await SendToSocket(item);
            }

            return sent;
        }

        public async Task<int> ReceiveAsync(Memory<byte> memory)
        {
            _readEventArgs.Reset(memory);

            try
            {
                if (!_socket.ReceiveAsync(_readEventArgs))
                {
                    _readEventArgs.Complete();
                    return 0;
                }
            }
            catch
            {
                _readEventArgs.Abandon();
                throw;
            }

            return await _readEventArgs;
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                _readEventArgs.Abandon();
                _writeEventArgs.Abandon();

                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }

                _socket.Dispose();
            }
        }

        async Task<int> SendToSocket(ReadOnlyMemory<byte> buffer)
        {
            _writeEventArgs.Reset(buffer);

            try
            {
                if (!_socket.SendAsync(_writeEventArgs))
                {
                    _writeEventArgs.Complete();
                    return 0;
                }
            }
            catch
            {
                _writeEventArgs.Abandon();
                throw;
            }

            return await _writeEventArgs;
        }

        ~SocketFacade() { Dispose(); }
    }
}
