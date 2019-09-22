using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    class SocketFacade : SocketContainer, IManagedSocket
    {
        readonly AwaitableSocketAsyncEventArgs _readEventArgs;
        readonly AwaitableSocketAsyncEventArgs _writeEventArgs;

        public SocketFacade(IServerEndpointFactory endPointFactory, TimeSpan timeout) : base(endPointFactory, timeout)
        {
            var scheduler = Scheduling.ThreadPoolScheduler.Instance;

            _readEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = EndPointAddress,
                CompletionHandler = w => scheduler.Schedule(() => { w(); return Task.CompletedTask; })
            };

            _writeEventArgs = new AwaitableSocketAsyncEventArgs()
            {
                RemoteEndPoint = EndPointAddress,
                CompletionHandler = w => scheduler.Schedule(() => { w(); return Task.CompletedTask; })
            };
        }

        public virtual async ValueTask<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            var sent = 0;

            if (buffer.IsEmpty)
            {
                return sent;
            }

            if (buffer.IsSingleSegment)
            {
                sent = await SendToSocket(buffer.First);
                return sent;
            }

            foreach (var item in buffer)
            {
                sent += await SendToSocket(item, SocketFlags.Partial);
            }

            return sent;
        }

        public virtual ValueTask<int> ReceiveAsync(Memory<byte> memory)
        {
#if NET_CORE
            return ReceiveCoreImplAsync(memory);
#else
            return ReceiveImplAsync(memory);
#endif
        }

#if NET_CORE
        async ValueTask<int> ReceiveCoreImplAsync(Memory<byte> memory)
        {
            OnReceiving(ReceiveStatus.Receiving);

            try
            {

                var task = Socket.ReceiveAsync(memory, SocketFlags.None, CancellationToken);
                var wasSync = false;

                if (task.IsCompleted)
                {
                    OnReceiving(ReceiveStatus.ReceivedSynchronously);
                    wasSync = true;
                }

                var read = await task;

                if (!wasSync)
                {
                    OnReceiving(ReceiveStatus.ReceivedAsynchronously);
                }

                OnReceiving(ReceiveStatus.Received);

                return read;
            }
            catch (Exception ex)
            {
                OnReceiving(ReceiveStatus.Faulted);
                State.ReadError(ex);
                throw;
            }
        }
#endif

        async ValueTask<int> ReceiveImplAsync(Memory<byte> memory)
        {
            if (Cancellation.IsCancellationRequested)
            {
                return 0;
            }

            OnReceiving(ReceiveStatus.CheckAvailable);

            await WaitForDataAsync();

            _readEventArgs.Reset(memory);

            var bytesRead = 0;

            while (bytesRead == 0)
            {
                try
                {
                    OnReceiving(ReceiveStatus.Receiving);

                    if (!Socket.ReceiveAsync(_readEventArgs))
                    {
                        _readEventArgs.Complete();

                        OnReceiving(ReceiveStatus.ReceivedSynchronously);
                    }
                    else
                    {
                        OnReceiving(ReceiveStatus.ReceivedAsynchronously);
                    }

                }
                catch (Exception ex)
                {
                    OnReceiving(ReceiveStatus.Faulted);

                    State.ReadError(ex);

                    _readEventArgs.Abandon();

                    throw;
                }

                OnReceiving(ReceiveStatus.Awaiting);

                bytesRead = await _readEventArgs;

                if (memory.IsEmpty || bytesRead > 0)
                {
                    break;
                }
            }

            OnReceiving(ReceiveStatus.Completed);

            OnTrace(() => (nameof(ReceiveAsync), memory.Slice(0, bytesRead).ToArray()));

            return bytesRead;
        }

        AwaitableSocketAsyncEventArgs WaitForDataAsync()
        {
            _readEventArgs.Reset(Memory<byte>.Empty);

            if (!Socket.ReceiveAsync(_readEventArgs))
            {
                _readEventArgs.Complete();
            }

            return _readEventArgs;
        }

#if NET_CORE
        async ValueTask<int> SendToSocket(ReadOnlyMemory<byte> buffer, SocketFlags flags = SocketFlags.None)
        {
            try
            {
                var result = await Socket.SendAsync(buffer, flags, CancellationToken);

                OnTrace(() => ($"{nameof(SendToSocket)},flags:{flags}", buffer.Slice(0, result).ToArray()));

                return result;
            }
            catch (Exception ex)
            {
                State.WriteError(ex);
                throw;
            }
        }
#else
        async ValueTask<int> SendToSocket(ReadOnlyMemory<byte> buffer, SocketFlags flags = SocketFlags.None)
        {
            if (Cancellation.IsCancellationRequested)
            {
                return 0;
            }

            if (buffer.IsEmpty)
            {
                return 0;
            }

            _writeEventArgs.Reset(buffer);
            _writeEventArgs.SocketFlags = flags;

            try
            {
                if (!Socket.SendAsync(_writeEventArgs))
                {
                    _writeEventArgs.Complete();
                }
            }
            catch (Exception ex)
            {
                State.WriteError(ex);

                _writeEventArgs.Abandon();

                throw;
            }

            var result = await _writeEventArgs;

            OnTrace(() => ($"{nameof(SendToSocket)},flags:{flags}", buffer.Slice(0, result).ToArray()));

            return result;
        }
#endif

        bool CheckConnected() => (Socket?.Connected).GetValueOrDefault();

        protected override void BeforeShutdown()
        {
            _readEventArgs.Abandon();
            _writeEventArgs.Abandon();
        }
    }
}
