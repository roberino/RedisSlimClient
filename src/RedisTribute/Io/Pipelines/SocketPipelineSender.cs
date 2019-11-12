using RedisTribute.Io.Net;
using RedisTribute.Io.Scheduling;
using RedisTribute.Types.Primatives;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Pipelines
{
    class SocketPipelineSender : IPipelineSender, ISchedulable
    {
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;
        readonly ResetHandle _resetHandle;
        readonly MemoryCursor _memoryCursor;

        public SocketPipelineSender(ISocket socket, CancellationToken cancellationToken, ResetHandle resetHandle)
        {
            _cancellationToken = cancellationToken;
            _resetHandle = resetHandle;
            _socket = socket;

            _pipe = new Pipe();

            _memoryCursor = new MemoryCursor(_pipe.Writer);
            _resetHandle.Resetting.Subscribe(Reset);
        }

        public Uri EndpointIdentifier => _socket.EndpointIdentifier;
        public event Action<Exception> Error;
        public event Action<PipelineStatus> StateChanged;
        public event Action<(string Action, byte[] Data)> Trace;

        public void Schedule(IWorkScheduler scheduler)
        {
            scheduler.Schedule(RunAsync);
        }

        public async Task RunAsync()
        {
            await PumpToSocket();
        }

        public async ValueTask SendAsync(byte[] data)
        {
            await _resetHandle.AwaitReset();

            StateChanged?.Invoke(PipelineStatus.WritingToPipe);

            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data), _cancellationToken).ConfigureAwait(false);

            StateChanged?.Invoke(PipelineStatus.WritingToPipeComplete);
        }

        public async ValueTask SendAsync(Func<IMemoryCursor, ValueTask> writeAction, CancellationToken cancellationToken = default)
        {
            await _resetHandle.AwaitReset();

            cancellationToken.ThrowIfCancellationRequested();

            await writeAction(_memoryCursor);

            StateChanged?.Invoke(PipelineStatus.WritingToPipe);

            await _memoryCursor.FlushAsync().ConfigureAwait(false);

            StateChanged?.Invoke(PipelineStatus.WritingToPipeComplete);
        }

        void Reset()
        {
            StateChanged?.Invoke(PipelineStatus.Resetting);

            _pipe.Reader.CancelPendingRead();
            _pipe.Writer.CancelPendingFlush();
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
            _pipe.Reset();
        }

        async Task PumpToSocket()
        {
            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    await _resetHandle.AwaitReset();

                    StateChanged?.Invoke(PipelineStatus.ReadingFromPipe);

                    var result = await _pipe.Reader.ReadAsync(_cancellationToken).ConfigureAwait(false);

                    if (IsRunning)
                    {
                        StateChanged?.Invoke(PipelineStatus.SendingToSocket);

                        var bytes = await _socket.SendAsync(result.Buffer).ConfigureAwait(false);

                        StateChanged?.Invoke(PipelineStatus.AdvancingWriter);

                        _pipe.Reader.AdvanceTo(result.Buffer.GetPosition(bytes));
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    error = ex;
                    StateChanged?.Invoke(PipelineStatus.Faulted);
                    Error?.Invoke(error);
                    _resetHandle.NotifyFault();
                    await Task.Delay(10);
                }
            }

            _pipe.Reader.Complete(error);
        }

        bool IsRunning => (!_cancellationToken.IsCancellationRequested);

        public void Dispose()
        {
            Error = null;
            StateChanged = null;

            try
            {
                _pipe.Reader.Complete();
                _pipe.Writer.Complete();
                _pipe.Reset();
            }
            catch { }
        }
    }
}