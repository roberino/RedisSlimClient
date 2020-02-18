using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using RedisTribute.Io.Pipelines;
using RedisTribute.Io.Scheduling;
using Xunit;

namespace RedisTribute.UnitTests.Io.Pipelines
{
    public class SocketPipelineReceiverTests
    {
        [Fact]
        public async Task SendAsync_SomeData_InvokesHandler()
        {
            var eventFired = false;
            byte[] capturedData = default;

            using (var socket = new StubSocket())
            {
                var waitHandle = new ManualResetEvent(false);
                var cancellationTokenSource = new CancellationTokenSource();
                var receiver = new SocketPipelineReceiver(socket, cancellationTokenSource.Token, new ResetHandle());

                await socket.SendStringAsync("abcxefg");

                receiver.RegisterHandler(s => s.PositionOf((byte)'x'), x =>
                {
                    eventFired = true;
                    capturedData = x.ToArray();
                    waitHandle.Set();
                });

                receiver.ScheduleOnThreadpool();

                socket.WaitForDataRead();
                waitHandle.WaitOne(30000);

                cancellationTokenSource.Cancel();
            }

            Assert.True(eventFired);
            Assert.Equal(4, capturedData.Length);
            Assert.Equal((byte)'a', capturedData[0]);
            Assert.Equal((byte)'b', capturedData[1]);
            Assert.Equal((byte)'c', capturedData[2]);
        }
    }
}
