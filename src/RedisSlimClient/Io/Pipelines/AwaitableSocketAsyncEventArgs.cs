using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RedisSlimClient.Io.Pipelines
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
            _isCompleted = false;
        }

        public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

        public Action<Action> CompletionHandler { get; set; }

        public bool IsCompleted => _isCompleted;

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

            _onCompleted = continuation;
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            _isCompleted = true;
        }

        public void Abandon()
        {
            _isCompleted = true;
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);

            _isCompleted = true;
        }

        ArraySegment<byte> GetArray(ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out var seg))
            {
                return seg;
            }

            return new ArraySegment<byte>(memory.ToArray());
        }
    }
}
