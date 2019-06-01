﻿using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class NetworkStreamFactory : INetworkStreamFactory
    {
        readonly EndPoint _endPoint;
        readonly Socket _socket;

        public NetworkStreamFactory(EndPoint endPoint)
        {
            _endPoint = endPoint;
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public Task ConnectAsync() => _socket.ConnectAsync(_endPoint);

        public async Task<Stream> CreateStreamAsync()
        {
            await ConnectAsync();

            return new NetworkStream(_socket, FileAccess.ReadWrite);
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}