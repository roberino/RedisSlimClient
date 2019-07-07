using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace RedisSlimClient.Io.Net
{
    class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        static readonly Memory<byte> NullMemory = new Memory<byte>();

        Action _onCompleted;
        volatile bool _isCompleted;

        public AwaitableSocketAsyncEventArgs()
        {
            Reset(NullMemory);
        }

        public void Reset(ReadOnlyMemory<byte> buffer)
        {
            var seg = GetArray(buffer);
            SetBuffer(seg.Array, seg.Offset, seg.Count);
            CompletionHandler = x => x();
            _isCompleted = false;
        }

        public CancellationToken Cancellation { get; set; }

        public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

        public Action<Action> CompletionHandler { get; set; }

        public bool IsCompleted => _isCompleted || Cancellation.IsCancellationRequested;

        public int GetResult()
        {
            return BytesTransferred;
        }

        public void OnCompleted(Action continuation)
        {
            if (_isCompleted)
            {
                CompletionHandler?.Invoke(continuation);
                return;
            }

            Interlocked.Exchange(ref _onCompleted, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            _isCompleted = true;

            Continue();
        }

        public void Abandon()
        {
            Complete();
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);

            Complete();
        }

        ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var seg))
            {
                return seg;
            }

            return new ArraySegment<byte>(memory.ToArray());
        }

        void Continue()
        {
            var completion = Interlocked.Exchange(ref _onCompleted, null);

            if (completion != null)
            {
                CompletionHandler?.Invoke(completion);
            }
        }
    }
}
