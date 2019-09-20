using RedisTribute.Configuration;
using System;
using System.Buffers;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    class SslSocket : SocketContainer, IManagedSocket
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

        public async ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
            if (_sslStream == null)
            {
                throw new InvalidOperationException();
            }

            var read = 0;

            OnReceiving(ReceiveStatus.Awaiting);

            try
            {
                read = await ReceiveAsyncImpl(memory);
            }
            catch (Exception ex)
            {
                OnReceiving(ReceiveStatus.Faulted);
                State.ReadError(ex);
                throw;
            }

            OnReceiving(ReceiveStatus.Completed);

            OnTrace(() => (nameof(ReceiveAsync), memory.Slice(0, read).ToArray()));

            return read;
        }

        public async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            if (_sslStream == null)
            {
                throw new InvalidOperationException();
            }

            int len = 0;

#if NET_CORE
            len = await SendCoreAsync(buffer);
#else

            if (buffer.Length > _writeBuffer.Length)
            {
                throw new NotSupportedException();
            }

            len = await SendBufferedAsync(buffer);
#endif

            OnTrace(() => (nameof(SendAsync), buffer.Slice(0, len).ToArray()));

            return len;
        }

        async ValueTask<int> SendBufferedAsync(ReadOnlySequence<byte> buffer)
        {
            var len = (int)buffer.Length;

            buffer.CopyTo(_writeBuffer);

            try
            {
                await _sslStream.WriteAsync(_writeBuffer, 0, len);
            }
            catch (Exception ex)
            {
                State.WriteError(ex);
                throw;
            }

            return len;
        }

#if NET_CORE
        async ValueTask<int> SendCoreAsync(ReadOnlySequence<byte> buffer)
        {
            try
            {
                if (buffer.IsSingleSegment)
                {
                    await _sslStream.WriteAsync(buffer.First);
                }
                else
                {
                    foreach (var span in buffer)
                    {
                        await _sslStream.WriteAsync(span);
                    }
                }
            }
            catch (Exception ex)
            {
                State.WriteError(ex);
                throw;
            }

            return (int)buffer.Length;
        }
#endif

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

        async ValueTask<int> ReceiveAsyncImpl(Memory<byte> memory)
        {
            var read = 0;

#if NET_CORE
            read = await _sslStream.ReadAsync(memory, CancellationToken);
#else

            if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out var seg))
            {
                read = await _sslStream.ReadAsync(seg.Array, seg.Offset, seg.Count);
            }
            else
            {
                read = await _sslStream.ReadAsync(_readBuffer, 0, memory.Length);

                for (var i = 0; i < read; i++)
                {
                    memory.Span[i] = _readBuffer[i];
                }
            }
#endif

            return read;
        }
    }
}