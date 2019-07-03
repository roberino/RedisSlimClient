using System;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketState
    {
        readonly Func<bool> _connectedState;

        SocketStatus _knownStatus;

        public event Action<SocketStatus> Changed;

        public async Task DoConnect(Func<Task> connectAsync)
        {
            ChangeStatus(SocketStatus.Connecting);

            try
            {
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

        void ChangeStatus(SocketStatus status)
        {
            _knownStatus = status;
            Changed?.Invoke(status);
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
        Terminated
    }
}