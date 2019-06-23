﻿using RedisSlimClient.Io.Pipelines;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    public class SocketPipelineReceiverTests
    {
        [Fact]
        public async Task SendAsync_SomeData_FiresReceievedEvent()
        {
            var eventFired = false;
            var socket = new StubSocket();
            var waitHandle = new ManualResetEvent(false);
            var cancellationTokenSource = new CancellationTokenSource();
            var receiver = new SocketPipelineReceiver(socket, cancellationTokenSource.Token);

            ReadOnlySequence<byte> capturedData = default;

            await socket.SendStringAsync("abcxefg");

            receiver.RegisterHandler(s => s.PositionOf((byte)'x'), x =>
            {
                eventFired = true;
                capturedData = x;
                waitHandle.Set();
            });

            receiver.ScheduleOnThreadpool();
            
            socket.WaitForDataRead();
            waitHandle.WaitOne(3000);

            cancellationTokenSource.Cancel();

            Assert.True(eventFired);
            Assert.Equal(4, capturedData.Length);
            Assert.Equal((byte)'a', capturedData.First.Span[0]);
            Assert.Equal((byte)'b', capturedData.First.Span[1]);
            Assert.Equal((byte)'c', capturedData.First.Span[2]);
        }
    }
}
