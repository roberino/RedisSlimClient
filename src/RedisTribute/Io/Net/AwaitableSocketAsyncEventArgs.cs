using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    /// <summary>
    /// Based on https://github.com/aspnet/AspNetCore/blob/master/src/Servers/Kestrel/Transport.Sockets/src/Internal/SocketAwaitableEventArgs.cs
    /// </summary>
    class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        static readonly Memory<byte> NullMemory = new Memory<byte>();

        static readonly Action _callbackCompleted = () => { };

        Action? _onCompleted;

        public AwaitableSocketAsyncEventArgs()
        {
            Reset(NullMemory);
        }

        public void Reset(Memory<byte> buffer)
        {
#if NET_CORE
            SetBuffer(buffer);
#else
            var seg = GetArray(buffer);
            SetBuffer(seg.Array, seg.Offset, seg.Count);
#endif
            CompletionHandler = x => x();
            _onCompleted = null;
        }

        public void Reset(ReadOnlyMemory<byte> buffer)
        {
#if NET_CORE
            SetBuffer(MemoryMarshal.AsMemory(buffer));
#else
            var seg = GetArray(buffer);
            SetBuffer(seg.Array, seg.Offset, seg.Count);
#endif
            CompletionHandler = x => x();
            _onCompleted = null;
        }

        public CancellationToken Cancellation { get; set; }

        public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

        public Action<Action>? CompletionHandler { get; set; }

        public bool IsCompleted =>
            ReferenceEquals(_onCompleted, _callbackCompleted)
            || Cancellation.IsCancellationRequested;

        public int GetResult()
        {
            _onCompleted = null;

            if (SocketError != SocketError.Success)
            {
                throw new SocketException((int)SocketError);
            }

            if (Cancellation.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return BytesTransferred;
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_onCompleted, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _onCompleted, continuation, null), _callbackCompleted))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            OnCompleted(this);
        }

        public void Abandon()
        {
            Complete();
        }

        //protected override void OnCompleted(SocketAsyncEventArgs e)
        //{
        //    base.OnCompleted(e);

        //    Continue();
        //}

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            var continuation = Interlocked.Exchange(ref _onCompleted, _callbackCompleted);

            if (continuation != null)
            {
                PipeScheduler.ThreadPool.Schedule(state => ((Action)state!)(), continuation);
            }
        }

        void Continue()
        {
            var continuation = Interlocked.Exchange(ref _onCompleted, _callbackCompleted);

            if (continuation != null)
            {
                CompletionHandler?.Invoke(continuation);
            }
        }

        static ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var seg))
            {
                return seg;
            }

            return new ArraySegment<byte>(memory.ToArray());
        }

        static ArraySegment<byte> GetArray(Memory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)memory, out var seg))
            {
                return seg;
            }

            throw new InvalidOperationException();
        }
    }
}