using RedisSlimClient.Io.Pipelines;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Pipelines
{
    public class SocketPipelineSenderTests
    {
        [Fact]
        public async Task SendAsync_SomeAction_WritesDataToSocket()
        {
            var socket = new StubSocket();
            var cancellationTokenSource = new CancellationTokenSource();
            var sender = new SocketPipelineSender(socket, cancellationTokenSource.Token);

            await sender.SendAsync(async m =>
            {
                foreach (var n in Enumerable.Range(0, 3))
                {
                    await m.Write((byte)(n + 1));
                }
            });

            TestExtensions.RunOnBackgroundThread(sender.RunAsync);

            socket.WaitForDataWrite();

            cancellationTokenSource.Cancel();

            var data = socket.Received.Single().ToArray();

            Assert.Equal(3, data.Length);
            Assert.Equal(1, data[0]);
            Assert.Equal(2, data[1]);
            Assert.Equal(3, data[2]);
        }
    }
}