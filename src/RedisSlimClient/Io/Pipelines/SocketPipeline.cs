using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipeline : IDuplexPipeline
    {
        readonly ISocket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;

        public SocketPipeline(EndPoint endPoint, TimeSpan timeout, Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, int minBufferSize = 512)
            : this(new SocketFacade(endPoint, timeout), delimitter, minBufferSize)
        {
        }

        public SocketPipeline(ISocket socket, Func<ReadOnlySequence<byte>, SequencePosition?> delimitter, int minBufferSize = 512)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _socket = socket;

            Receiver = new SocketPipelineReceiver(_socket, _cancellationTokenSource.Token, delimitter, minBufferSize);
            Sender = new SocketPipelineSender(_socket, _cancellationTokenSource.Token);
        }

        public IPipelineReceiver Receiver { get; }

        public IPipelineSender Sender { get; }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();

                Receiver.Dispose();
                Sender.Dispose();

                _socket.Dispose();
            }
        }

        public Task RunAsync()
        {
            return Task.WhenAll(((IRunnable)Receiver).RunAsync(), ((IRunnable)Sender).RunAsync());
        }

        ~SocketPipeline() { Dispose(); }
    }
}