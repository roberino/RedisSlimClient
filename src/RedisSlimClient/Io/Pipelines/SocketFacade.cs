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
        Socket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly AwaitableSocketAsyncEventArgs _readEventArgs;
        readonly AwaitableSocketAsyncEventArgs _writeEventArgs;
        readonly EndPoint _endPoint;
        readonly TimeSpan _timeout;

        public SocketFacade(EndPoint endPoint, TimeSpan timeout)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            _readEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = endPoint
            };

            _writeEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = endPoint
            };

            _endPoint = endPoint;
            _timeout = timeout;

            State = new SocketState(CheckConnected);
        }

        protected Socket Socket => _socket;

        public SocketState State { get; }

        public virtual Task ConnectAsync()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(SocketFacade));
            }

            if (_socket == null)
            {
                InitialiseSocket();
            }

            return State.DoConnect(() => _socket.ConnectAsync(_endPoint));
        }

        public virtual async Task<int> SendAsync(ReadOnlySequence<byte> buffer)
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

        public virtual async Task<int> ReceiveAsync(Memory<byte> memory)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return 0;
            }

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

                ShutdownSocket();

                OnDisposing();
            }
        }

        protected virtual void OnDisposing()
        {
        }

        async Task<int> SendToSocket(ReadOnlyMemory<byte> buffer)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return 0;
            }

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

        bool CheckConnected() => (_socket?.Connected).GetValueOrDefault();

        void ShutdownSocket()
        {
            _readEventArgs.Abandon();
            _writeEventArgs.Abandon();

            Try(() => _socket.Shutdown(SocketShutdown.Send));
            Try(() => _socket.Shutdown(SocketShutdown.Receive));
            Try(_socket.Close);
            Try(_socket.Dispose);
        }

        void InitialiseSocket()
        {
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)_timeout.TotalMilliseconds,
                SendTimeout = (int)_timeout.TotalMilliseconds,
                NoDelay = true
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        void Try(Action act)
        {
            try { act(); }
            catch { }
        }

        ~SocketFacade() { Dispose(); }
    }
}