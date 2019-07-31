using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Net
{
    class SocketFacade : IManagedSocket
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

            var scheduler = Scheduling.ThreadPoolScheduler.Instance;

            _readEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = _endPoint,
                CompletionHandler = w => scheduler.Schedule(() => { w(); return Task.CompletedTask; })
            };

            _writeEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = _endPoint,
                CompletionHandler = w => scheduler.Schedule(() => { w(); return Task.CompletedTask; })
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

        public event Action<ReceiveStatus> Receiving;

        public virtual Task ConnectAsync()
        {
            return InitSocketAndNotifyAsync();
        }
        
        public async Task AwaitAvailableSocket(CancellationToken cancellation)
        {
            while (!State.IsAvailable && !cancellation.IsCancellationRequested)
            {
                await Task.Delay(5, cancellation);
            }
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

            //if (_socket.Available == 0 && !memory.IsEmpty)
            //{
            //    OnReceiving(ReceiveStatus.CheckAvailable);

            //    await ReceiveAsync(default);
            //}

            _readEventArgs.Reset(memory);

            var bytesRead = 0;

            while (bytesRead == 0)
            {
                try
                {
                    if (!_socket.ReceiveAsync(_readEventArgs))
                    {
                        _readEventArgs.Complete();

                        OnReceiving(ReceiveStatus.ReceivedSynchronously);
                    }
                    else
                    {
                        OnReceiving(ReceiveStatus.ReceivedAsynchronously);
                    }

                }
                catch (Exception ex)
                {
                    OnReceiving(ReceiveStatus.Faulted);

                    State.ReadError(ex);

                    _readEventArgs.Abandon();

                    throw;
                }

                OnReceiving(ReceiveStatus.Awaiting);

                bytesRead = await _readEventArgs;

                if (memory.IsEmpty)
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    await Task.Delay(1);
                }
            }

            OnReceiving(ReceiveStatus.Completed);

            return bytesRead;
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Receiving = null;

                State.Terminated();

                _cancellationTokenSource.Cancel();

                ShutdownSocket();

                _cancellationTokenSource.Dispose();

                State.Dispose();

                OnDisposing();
            }
        }

        protected void OnReceiving(ReceiveStatus status)
        {
            Receiving?.Invoke(status);
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

            if (buffer.IsEmpty)
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
                ReceiveBufferSize = 8192,
                SendBufferSize = 8192,
                NoDelay = true,
                ExclusiveAddressUse = true
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