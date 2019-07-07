using RedisSlimClient.Configuration;
using RedisSlimClient.Io.Scheduling;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipeline : IDuplexPipeline
    {
        readonly ISocket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;

        public SocketPipeline(EndPoint endPoint, TimeSpan timeout, IReadWriteBufferSettings bufferSettings)
            : this(new SocketFacade(endPoint, timeout), bufferSettings)
        {
        }

        public SocketPipeline(ISocket socket, IReadWriteBufferSettings bufferSettings = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _socket = socket;

            Receiver = new SocketPipelineReceiver(_socket, _cancellationTokenSource.Token, (bufferSettings?.ReadBufferSize).GetValueOrDefault(512));
            Sender = new SocketPipelineSender(_socket, _cancellationTokenSource.Token);

            _socket.State.Changed += OnSocketChange;
        }

        public IPipelineReceiver Receiver { get; }

        public IPipelineSender Sender { get; }

        public event Action Faulted;

        public Task RunAsync()
        {
            return Task.WhenAll(Runnables.Select(x => x.RunAsync()));
        }

        public async Task Reset()
        {
            foreach (var runnable in Runnables)
                await runnable.Reset();

            await _socket.ConnectAsync();
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();

                Receiver.Dispose();
                Sender.Dispose();

                _socket.Dispose();

                _cancellationTokenSource.Dispose();
            }
        }

        void OnSocketChange(SocketStatus e)
        {
            if (e == SocketStatus.ReadFault || e == SocketStatus.WriteFault)
            {
                Faulted?.Invoke();
            }
        }

        ~SocketPipeline() { Dispose(); }

        IRunnable[] Runnables => new[] { (IRunnable)Receiver, (IRunnable)Sender };
    }
}