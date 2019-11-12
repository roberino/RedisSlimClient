using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Configuration;
using RedisTribute.Io.Net;
using RedisTribute.Io.Scheduling;
using RedisTribute.Util;

namespace RedisTribute.Io.Pipelines
{
    class SocketPipeline : IDuplexPipeline
    {
        readonly ISocket _socket;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly ResetHandle _resetHandle;

        public SocketPipeline(ISocket socket, IReadWriteBufferSettings bufferSettings = null)
        {
            _socket = socket;

            _cancellationTokenSource = new CancellationTokenSource();
            _resetHandle = new ResetHandle();

            Receiver = new SocketPipelineReceiver(_socket, _cancellationTokenSource.Token, _resetHandle, (bufferSettings?.ReadBufferSize).GetValueOrDefault(512));
            Sender = new SocketPipelineSender(_socket, _cancellationTokenSource.Token, _resetHandle);

            _socket.State.Changed += OnSocketChange;
            _resetHandle.Fault += OnFaultNotification;
        }

        public IPipelineReceiver Receiver { get; }

        public IPipelineSender Sender { get; }

        public event Action Faulted;

        public Task RunAsync()
        {
            _resetHandle.Enable();

            return Task.WhenAll(Schedulables.Select(x => x.RunAsync()));
        }

        public void Schedule(IWorkScheduler scheduler)
        {
            scheduler.Schedule(RunAsync);
        }

        public async Task ResetAsync()
        {
            using (await _resetHandle.BeginReset())
            {
                await Attempt.WithExponentialBackoff(_socket.ConnectAsync, TimeSpan.FromSeconds(5), cancellation: _cancellationTokenSource.Token);
            }
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
                _resetHandle.Dispose();
            }
        }

        void OnFaultNotification()
        {
            Faulted?.Invoke();
        }

        void OnSocketChange((SocketStatus status, long id) state)
        {
            if (state.status == SocketStatus.WriteFault)
            {
                Faulted?.Invoke();
            }
        }

        ~SocketPipeline() { Dispose(); }

        ISchedulable[] Schedulables => new[] { (ISchedulable)Receiver, (ISchedulable)Sender };
    }
}