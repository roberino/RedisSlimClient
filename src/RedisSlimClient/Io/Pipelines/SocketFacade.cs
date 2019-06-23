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
        readonly EndPoint _endPoint;

        public SocketFacade(EndPoint endPoint, TimeSpan timeout)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)timeout.TotalMilliseconds,
                SendTimeout = (int)timeout.TotalMilliseconds,
                NoDelay = true,
                Blocking = false
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _readEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = endPoint
            };

            _writeEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = endPoint
            };

            _endPoint = endPoint;

            State = new SocketState(() => _socket.Connected);
        }

        public SocketState State { get; }

        public Task ConnectAsync()
        {
            return State.DoConnect(() => _socket.ConnectAsync(_endPoint));
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
                sent = await SendToSocket(buffer.First);
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
            if (_socket.Available == 0 && !memory.IsEmpty)
            {
                await ReceiveAsync(default);
            }

            _readEventArgs.Reset(memory);

            try
            {  
                if (!_socket.ReceiveAsync(_readEventArgs))
                {
                    _readEventArgs.Complete();
                }
            }
            catch (Exception ex)
            {
                State.ReadError(ex);

                _readEventArgs.Abandon();

                throw;
            }

            return await _readEventArgs;
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                State.Terminated();

                _cancellationTokenSource.Cancel();
                _readEventArgs.Abandon();
                _writeEventArgs.Abandon();

                Try(() => _socket.Shutdown(SocketShutdown.Receive));
                Try(() => _socket.Shutdown(SocketShutdown.Send));
                Try(_socket.Close);
                Try(_socket.Dispose);
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
                }
            }
            catch (Exception ex)
            {
                State.WriteError(ex);

                _writeEventArgs.Abandon();

                throw;
            }

            return await _writeEventArgs;
        }

        void Try(Action act)
        {
            try { act(); }
            catch { }
        }

        ~SocketFacade() { Dispose(); }
    }
}