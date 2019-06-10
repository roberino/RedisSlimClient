using RedisSlimClient.Io.Pipelines;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    class StubSocket : ISocket
    {
        readonly ManualResetEvent _waitHandle;

        public StubSocket()
        {
            _waitHandle = new ManualResetEvent(false);
            Received = new List<ReadOnlySequence<byte>>();
        }

        public void Dispose()
        {
            _waitHandle.Dispose();
        }

        public Task<int> ReceiveAsync(Memory<byte> memory)
        {
            throw new NotImplementedException();
        }

        public Task<int> SendAsync(ReadOnlySequence<byte> buffer)
        {
            Received.Add(buffer);

            _waitHandle.Set();

            return Task.FromResult((int)buffer.Length);
        }

        public void WaitForDataWrite() => _waitHandle.WaitOne();

        public IList<ReadOnlySequence<byte>> Received { get; }
    }
}
