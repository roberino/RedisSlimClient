using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    class Connection : IDisposable
    {
        readonly EndPoint _endPoint;
        readonly Socket _socket;

        NetworkStream _stream;
        CommandPipeline _commandPipeline;

        public Connection(EndPoint endPoint)
        {
            _endPoint = endPoint;

            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task<ICommandPipeline> ConnectAsync()
        {
            if (_commandPipeline == null)
            {
                await _socket.ConnectAsync(_endPoint);

                _stream = new NetworkStream(_socket, FileAccess.ReadWrite);
                _commandPipeline = new CommandPipeline(_stream);
            }

            return _commandPipeline;
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _stream?.Dispose();
        }
    }
}
