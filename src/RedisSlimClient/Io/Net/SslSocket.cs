using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Pipelines;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Net
{
    class SslSocket : SocketFacade
    {
        readonly SslConfiguration _configuration;
        readonly byte[] _writeBuffer;
        readonly byte[] _readBuffer;

        Stream _sslStream;

        public SslSocket(IServerEndpointFactory endPointFactory, TimeSpan timeout, SslConfiguration configuration, IReadWriteBufferSettings bufferSettings) : base(endPointFactory, timeout)
        {
            _configuration = configuration;
            _readBuffer = new byte[bufferSettings.ReadBufferSize];
            _writeBuffer = new byte[bufferSettings.WriteBufferSize];
        }

        public override async Task<Stream> CreateStream()
        {
            var socketStream = await base.CreateStream();

            var sslStream = new SslStream(socketStream, false, _configuration.RemoteCertificateValidationCallback, _configuration.ClientCertificateValidationCallback, EncryptionPolicy.RequireEncryption);

            await sslStream.AuthenticateAsClientAsync(_configuration.SslHost);

            return sslStream;
        }

        public override async Task ConnectAsync()
        {
            _sslStream = await CreateStream();
        }

        public override
#if !NET_CORE             
            async
#endif
             ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
            if (_sslStream == null)
            {
                throw new InvalidOperationException();
            }

#if NET_CORE
            return _sslStream.ReadAsync(memory, CancellationToken);
#else

            if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out var seg))
            {
                var read = await _sslStream.ReadAsync(seg.Array, seg.Offset, seg.Count);

                return read;
            }
            else
            {
                var read = await _sslStream.ReadAsync(_readBuffer, 0, memory.Length);

                for (var i = 0; i < read; i++)
                {
                    memory.Span[i] = _readBuffer[i];
                }

                return read;
            }
#endif
        }

        public override async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
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

        protected override void OnDisposing()
        {
            base.OnDisposing();

            if (_sslStream != null)
            {
                try
                {
                    _sslStream.Dispose();
                }
                catch { }
                _sslStream = null;
            }
        }
    }
}