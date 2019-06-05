using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RedisSlimClient.Io.Pipelines
{
    class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        Action _onCompleted;
        volatile bool _isCompleted;

        public AwaitableSocketAsyncEventArgs(Memory<byte> buffer)
        {
            Reset(buffer);
        }

        public void Reset(Memory<byte> buffer)
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

        ArraySegment<byte> GetArray(Memory<byte> memory)
        {
            throw new NotImplementedException();
        }
    }
}
