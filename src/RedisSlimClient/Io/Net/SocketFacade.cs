using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RedisSlimClient.Telemetry;

namespace RedisSlimClient.Io.Net
{
    class SocketFacade : IManagedSocket, ITraceable
    {
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

        public Socket Socket { get; private set; }

        public virtual async Task<Stream> CreateStream()
        {
            if (!State.IsConnected)
            {
                await InitSocketAndNotifyAsync();
            }

            return new NetworkStream(Socket, FileAccess.ReadWrite)
            {
                ReadTimeout = (int)_timeout.TotalMilliseconds,
                WriteTimeout = (int)_timeout.TotalMilliseconds
            };
        }

        public SocketState State { get; }

        public event Action<ReceiveStatus> Receiving;

        public event Action<(string Action, byte[] Data)> Trace;

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
                sent += await SendToSocket(item, SocketFlags.Partial);
            }

            return sent;
        }

        public virtual ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
#if NET_CORE
            return ReceiveCoreImplAsync(memory);
#else
            return ReceiveImplAsync(memory);
#endif
        }

        public void Dispose()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            Receiving = null;

            State.Terminated();

            _cancellationTokenSource.Cancel();

            ShutdownSocket();

            _cancellationTokenSource.Dispose();

            State.Dispose();

            OnDisposing();
        }

        protected void OnReceiving(ReceiveStatus status)
        {
            Receiving?.Invoke(status);
        }

        protected void OnTrace(Func<(string name, byte[] data)> traceAction)
        {
            Trace?.Invoke(traceAction());
        }

        protected virtual void OnDisposing()
        {
        }

#if NET_CORE
        async ValueTask<int> ReceiveCoreImplAsync(Memory<byte> memory)
        {
            OnReceiving(ReceiveStatus.Receiving);

            var task = Socket.ReceiveAsync(memory, SocketFlags.None, CancellationToken);
            var wasSync = false;

            if (task.IsCompleted)
            {
                OnReceiving(ReceiveStatus.ReceivedSynchronously);
                wasSync = true;
            }

            var read = await task;

            if (!wasSync)
            {
                OnReceiving(ReceiveStatus.ReceivedAsynchronously);
            }

            OnReceiving(ReceiveStatus.Received);

            return read;
        }
#endif

        async ValueTask<int> ReceiveImplAsync(Memory<byte> memory)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return 0;
            }

            OnReceiving(ReceiveStatus.CheckAvailable);

            await WaitForDataAsync();

            _readEventArgs.Reset(memory);

            var bytesRead = 0;

            while (bytesRead == 0)
            {
                try
                {
                    OnReceiving(ReceiveStatus.Receiving);

                    if (!Socket.ReceiveAsync(_readEventArgs))
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

                if (memory.IsEmpty || bytesRead > 0)
                {
                    break;
                }
            }

            OnReceiving(ReceiveStatus.Completed);
            
            Trace?.Invoke((nameof(ReceiveAsync), memory.Slice(0, bytesRead).ToArray()));

            return bytesRead;
        }

        AwaitableSocketAsyncEventArgs WaitForDataAsync()
        {
            _readEventArgs.Reset(Memory<byte>.Empty);

            if (!Socket.ReceiveAsync(_readEventArgs))
            {
                _readEventArgs.Complete();
            }

            return _readEventArgs;
        }

#if NET_CORE
        async ValueTask<int> SendToSocket(ReadOnlyMemory<byte> buffer, SocketFlags flags = SocketFlags.None)
        {
            var result = await Socket.SendAsync(buffer, flags, CancellationToken);

            Trace?.Invoke(($"{nameof(SendToSocket)},flags:{flags}", buffer.Slice(0, result).ToArray()));

            return result;
        }
#else
        async ValueTask<int> SendToSocket(ReadOnlyMemory<byte> buffer, SocketFlags flags = SocketFlags.None)
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
            _writeEventArgs.SocketFlags = flags;

            try
            {
                if (!Socket.SendAsync(_writeEventArgs))
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

            var result = await _writeEventArgs;

            Trace?.Invoke(($"{nameof(SendToSocket)},flags:{flags}", buffer.Slice(0, result).ToArray()));

            return result;
        }
#endif

        bool CheckConnected() => (Socket?.Connected).GetValueOrDefault();

        void ShutdownSocket()
        {
            var socket = Socket;

            Socket = null;
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

            return State.DoConnect(() => Socket.ConnectAsync(_endPoint));
        }

        void InitialiseSocket()
        {
            if (Socket != null)
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

            Socket = socket;
        }

        void Try(Action act)
        {
            try { act(); }
            catch { }
        }

        ~SocketFacade() { Dispose(); }
    }
}