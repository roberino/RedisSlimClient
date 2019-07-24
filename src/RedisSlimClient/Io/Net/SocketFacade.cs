using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Net
{
    class SocketFacade : ISocket, IManagedSocket
    {
        Socket _socket;

        readonly CancellationTokenSource _cancellationTokenSource;
        readonly AwaitableSocketAsyncEventArgs _readEventArgs;
        readonly AwaitableSocketAsyncEventArgs _writeEventArgs;
        readonly EndPoint _endPoint;
        readonly IServerEndpointFactory _endPointFactory;
        readonly TimeSpan _timeout;

        public SocketFacade(IServerEndpointFactory endPointFactory, TimeSpan timeout)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _endPoint = endPointFactory.CreateEndpoint();

            _readEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = _endPoint
            };

            _writeEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = _endPoint
            };
            _endPointFactory = endPointFactory;
            _timeout = timeout;

            State = new SocketState(CheckConnected);
        }

        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public Uri EndpointIdentifier => _endPointFactory.EndpointIdentifier;

        public Socket Socket => _socket;

        public virtual async Task<Stream> CreateStream()
        {
            if (!State.IsConnected)
            {
                await InitSocketAndNotifyAsync();
            }

            return new NetworkStream(_socket, FileAccess.ReadWrite)
            {
                ReadTimeout = (int)_timeout.TotalMilliseconds,
                WriteTimeout = (int)_timeout.TotalMilliseconds
            };
        }

        public SocketState State { get; }

        public virtual Task ConnectAsync()
        {
            return InitSocketAndNotifyAsync();
        }

        public virtual async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
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

        public virtual async ValueTask<int> ReceiveAsync(Memory<byte> memory)
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
            var socket = _socket;

            _socket = null;
            _readEventArgs.Abandon();
            _writeEventArgs.Abandon();

            Try(() => socket.Shutdown(SocketShutdown.Send));
            Try(() => socket.Shutdown(SocketShutdown.Receive));
            Try(socket.Close);
            Try(socket.Dispose);
        }

        Task InitSocketAndNotifyAsync()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(SocketFacade));
            }

            InitialiseSocket();

            return State.DoConnect(() => _socket.ConnectAsync(_endPoint));
        }

        void InitialiseSocket()
        {
            if (_socket != null)
            {
                ShutdownSocket();
            }

            var socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)_timeout.TotalMilliseconds,
                SendTimeout = (int)_timeout.TotalMilliseconds,
                NoDelay = true
            };

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _socket = socket;
        }

        void Try(Action act)
        {
            try { act(); }
            catch { }
        }

        ~SocketFacade() { Dispose(); }
    }
}