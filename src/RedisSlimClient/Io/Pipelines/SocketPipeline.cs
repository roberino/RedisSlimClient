using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipeline : IDuplexPipeline
    {
        readonly Socket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;

        public SocketPipeline(EndPoint endPoint, TimeSpan timeout, byte delimitter, int minBufferSize = 512)
        {
            _socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                ReceiveTimeout = (int)timeout.TotalMilliseconds,
                SendTimeout = (int)timeout.TotalMilliseconds,
                NoDelay = true
            };

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            Receiver = new SocketPipelineReceiver(_socket, _cancellationTokenSource.Token, delimitter, minBufferSize);
        }

        public IPipelineReceiver Receiver { get; }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Receiver.Dispose();

                _cancellationTokenSource.Cancel();

                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch { }

                _socket.Dispose();
            }
        }

        ~SocketPipeline() { Dispose(); }
    }
}