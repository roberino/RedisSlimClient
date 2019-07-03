using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using System;
using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Ssl
{
    class SslSocket : SocketFacade
    {
        readonly SslConfiguration _configuration;
        readonly byte[] _writeBuffer;
        readonly byte[] _readBuffer;

        SslStream _sslStream;

        public SslSocket(EndPoint endPoint, TimeSpan timeout, SslConfiguration configuration, IReadWriteBufferSettings bufferSettings) : base(endPoint, timeout)
        {
            _configuration = configuration;
            _readBuffer = new byte[bufferSettings.ReadBufferSize];
            _writeBuffer = new byte[bufferSettings.WriteBufferSize];
        }

        public override async Task ConnectAsync()
        {
            await base.ConnectAsync();

            var stream = new NetworkStream(Socket);

            _sslStream = new SslStream(stream, false, _configuration.RemoteCertificateValidationCallback);

            await _sslStream.AuthenticateAsClientAsync(_configuration.SslHost);
        }

        public override Task<int> ReceiveAsync(Memory<byte> memory)
        {
            throw new NotImplementedException();
        }

        public override async Task<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            if (_sslStream == null)
            {
                throw new InvalidOperationException();
            }

            var len = (int)buffer.Length;
            
            buffer.CopyTo(_writeBuffer);

            await _sslStream.WriteAsync(_writeBuffer, 0, len);

            return len;
        }
    }
}