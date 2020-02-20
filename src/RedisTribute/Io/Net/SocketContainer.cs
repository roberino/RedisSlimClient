using RedisTribute.Telemetry;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    class SocketContainer : ITraceable
    {
        readonly IServerEndpointFactory _endPointFactory;

        protected readonly CancellationTokenSource Cancellation;
        protected readonly TimeSpan ConnectTimeout;
        protected readonly EndPoint EndPointAddress;

        public SocketContainer(IServerEndpointFactory endPointFactory, TimeSpan timeout)
        {
            _endPointFactory = endPointFactory;

            Cancellation = new CancellationTokenSource();
            EndPointAddress = endPointFactory.CreateEndpoint();
            ConnectTimeout = timeout;

            State = new SocketState(CheckConnected);
        }

        protected CancellationToken CancellationToken => Cancellation.Token;

        public Uri EndpointIdentifier => _endPointFactory.EndpointIdentifier;

        public Socket Socket { get; private set; }

        public SocketState State { get; }

        public event Action<ReceiveStatus> Receiving;

        public event Action<(string Action, byte[] Data)> Trace;

        public virtual Task ConnectAsync()
        {
            return InitSocketAndNotifyAsync();
        }

        public virtual async Task<Stream> CreateStream()
        {
            if (!State.IsConnected)
            {
                await InitSocketAndNotifyAsync();
            }

            return new NetworkStream(Socket, FileAccess.ReadWrite)
            {
                ReadTimeout = (int)ConnectTimeout.TotalMilliseconds,
                WriteTimeout = (int)ConnectTimeout.TotalMilliseconds
            };
        }

        public async Task AwaitAvailableSocket(CancellationToken cancellation)
        {
            while (!State.IsAvailable && !cancellation.IsCancellationRequested)
            {
                Console.WriteLine($"Status: {State.Status}");
                await Task.Delay(5, cancellation);
            }
        }

        public void Dispose()
        {
            if (Cancellation.IsCancellationRequested)
            {
                return;
            }

            Receiving = null;

            State.Terminated();

            Cancellation.Cancel();

            ShutdownSocket();

            Cancellation.Dispose();

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
        protected virtual void BeforeShutdown()
        {
        }

        bool CheckConnected() => (Socket?.Connected).GetValueOrDefault();

        Task InitSocketAndNotifyAsync()
        {
            if (Cancellation.IsCancellationRequested)
            {
                throw new ObjectDisposedException(nameof(SocketFacade));
            }

            InitialiseSocket();

            return State.DoConnect(() => Socket.ConnectAsync(EndPointAddress));
        }

        void ShutdownSocket()
        {
            var socket = Socket;

            if (socket == null) return;

            Socket = null;

            BeforeShutdown();

            Try(() => socket.Shutdown(SocketShutdown.Send));
            Try(() => socket.Shutdown(SocketShutdown.Receive));
            Try(socket.Close);
            Try(socket.Dispose);
        }

        void InitialiseSocket()
        {
            if (Socket != null)
            {
                ShutdownSocket();
            }

            var socket = new Socket(EndPointAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)ConnectTimeout.TotalMilliseconds,
                SendTimeout = (int)ConnectTimeout.TotalMilliseconds,
                ReceiveBufferSize = 8192,
                SendBufferSize = 8192,
                LingerState = new LingerOption(false, 0)
            };

            Try(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true));

            Socket = socket;
        }

        void Try(Action act)
        {
            try { act(); }
            catch { }
        }

        ~SocketContainer() { Dispose(); }
    }
}