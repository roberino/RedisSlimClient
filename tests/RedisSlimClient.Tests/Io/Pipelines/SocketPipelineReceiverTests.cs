using RedisSlimClient.Io.Pipelines;
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
            var receiver = new SocketPipelineReceiver(socket, cancellationTokenSource.Token, (byte)'x');

            await socket.SendStringAsync("abcxefg");

            receiver.Received += x =>
            {
                eventFired = true;
                waitHandle.Set();

                Assert.Equal(3, x.Length);
                Assert.Equal((byte)'a', x.First.Span[0]);
                Assert.Equal((byte)'b', x.First.Span[1]);
                Assert.Equal((byte)'c', x.First.Span[2]);
            };

            TestExtensions.RunOnBackgroundThread(receiver.RunAsync);
            
            socket.WaitForDataRead();
            waitHandle.WaitOne(1000);

            cancellationTokenSource.Cancel();

            Assert.True(eventFired);
        }
    }
}
