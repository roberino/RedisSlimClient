using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Net
{
    class SocketState : IDisposable
    {
        long _connectionNumber;
        long _lastFauledConnectionNumber;

        readonly Func<bool> _connectedState;

        SocketStatus _knownStatus;

        public event Action<(SocketStatus Status, long Id)> Changed;

        public async Task DoConnect(Func<Task> connectAsync)
        {
            ChangeStatus(SocketStatus.Connecting);

            try
            {
                Interlocked.Increment(ref _connectionNumber);
                await connectAsync();
                ChangeStatus(SocketStatus.Connected);
            }
            catch (Exception ex)
            {
                LastException = ex;
                ChangeStatus(SocketStatus.ConnectFault);
                throw;
            }
        }

        public SocketState(Func<bool> connectedState)
        {
            _connectedState = connectedState;
        }

        public void ReadError(Exception ex)
        {
            LastException = ex;
            ChangeStatus(SocketStatus.ReadFault);
        }

        public void WriteError(Exception ex)
        {
            LastException = ex;
            ChangeStatus(SocketStatus.WriteFault);
        }

        public void Terminated()
        {
            ChangeStatus(SocketStatus.Terminated);
            Changed = null;
        }

        public bool IsFaulted => IsFaultedStatus(Status);

        public bool IsAvailable => Status == SocketStatus.Connected || Status == SocketStatus.Disconnected;

        public bool IsConnected => Status == SocketStatus.Connected;

        public Exception LastException { get; private set; }

        public SocketStatus Status
        {
            get
            {
                if (_knownStatus == SocketStatus.Connected)
                {
                    if (!_connectedState())
                    {
                        return SocketStatus.Terminated;
                    }
                }

                return _knownStatus;
            }
        }

        public long Id => Interlocked.Read(ref _connectionNumber);

        void ChangeStatus(SocketStatus status)
        {
            var currentId = Id;
            var lastError = -1L;

            if (IsFaultedStatus(status))
            {
                lastError = Interlocked.Exchange(ref _lastFauledConnectionNumber, currentId);
            }

            if (_knownStatus != status)
            {
                _knownStatus = status;

                Changed?.Invoke((status, currentId));

                return;
            }

            if (lastError != -1 && lastError != currentId)
            {
                Changed?.Invoke((status, currentId));
            }
        }

        static bool IsFaultedStatus(SocketStatus status) => status == SocketStatus.ConnectFault || status == SocketStatus.ReadFault || status == SocketStatus.WriteFault;

        public void Dispose()
        {
            Changed = null;
            _knownStatus = SocketStatus.Disposed;
        }
    }

    enum SocketStatus
    {
        Disconnected,
        Connecting,
        Connected,
        ConnectFault,
        ReadFault,
        WriteFault,
        Terminated,
        Disposed
    }
}