using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class NetworkStreamFactory : INetworkStreamFactory
    {
        readonly EndPoint _endPoint;
        readonly Socket _socket;

        bool _disposed;

        public NetworkStreamFactory(EndPoint endPoint, TimeSpan timeout)
        {
            _endPoint = endPoint;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)timeout.TotalMilliseconds,
                SendTimeout = (int)timeout.TotalMilliseconds,
                NoDelay = true
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        Task ConnectAsync() => _socket.ConnectAsync(_endPoint);

        public async Task<Stream> CreateStreamAsync()
        {
            await ConnectAsync();

            return new NetworkStream(_socket, FileAccess.ReadWrite);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }
                _socket.Dispose();
            }
        }

        ~NetworkStreamFactory() { Dispose(); }
    }
}