using RedisSlimClient.Io.Net;
using RedisSlimClient.Io.Scheduling;
using RedisSlimClient.Types.Primatives;
using RedisSlimClient.Util;
using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    class SocketPipelineSender : IPipelineSender, ISchedulable, IResetable
    {
        readonly ISocket _socket;
        readonly Pipe _pipe;
        readonly CancellationToken _cancellationToken;
        readonly MemoryCursor _memoryCursor;
        readonly AsyncLock _resetLock;

        volatile bool _resetting;

        public SocketPipelineSender(ISocket socket, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _socket = socket;

            _resetLock = new AsyncLock();
            _pipe = new Pipe();

            _memoryCursor = new MemoryCursor(_pipe.Writer);
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
            _resetting = false;

            await PumpToSocket();
        }

        public async Task<IDisposable> ResetAsync()
        {
            StateChanged?.Invoke(PipelineStatus.Resetting);

            var handle = await _resetLock.LockAsync();

            _resetting = true;

            _pipe.Reader.CancelPendingRead();
            _pipe.Writer.CancelPendingFlush();
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();
            _pipe.Reset();

            _resetting = false;

            return handle;
        }

        public async ValueTask SendAsync(byte[] data)
        {
            await AwaitReset();

            StateChanged?.Invoke(PipelineStatus.WritingToPipe);

            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(data), _cancellationToken).ConfigureAwait(false);

            StateChanged?.Invoke(PipelineStatus.WritingToPipeComplete);
        }

        public async ValueTask SendAsync(Func<IMemoryCursor, ValueTask> writeAction)
        {
            await AwaitReset();

            await writeAction(_memoryCursor);

            StateChanged?.Invoke(PipelineStatus.WritingToPipe);

            await _memoryCursor.FlushAsync().ConfigureAwait(false);

            StateChanged?.Invoke(PipelineStatus.WritingToPipeComplete);
        }

        async ValueTask AwaitReset()
        {
            while (_resetting)
            {
                StateChanged?.Invoke(PipelineStatus.AwaitingReset);
                await _resetLock.AwaitAsync();
            }
        }

        async Task PumpToSocket()
        {
            Exception error = null;

            while (IsRunning)
            {
                try
                {
                    await AwaitReset();

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
                catch (AggregateException ex)
                {
                    if (ex.InnerException is TaskCanceledException)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    StateChanged?.Invoke(PipelineStatus.Faulted);
                    Error?.Invoke(error);
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