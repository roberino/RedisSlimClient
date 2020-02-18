using RedisTribute.Io.Pipelines;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Io.Pipelines
{
    public class SocketPipelineSenderTests
    {
        [Fact]
        public async Task SendAsync_SomeAction_WritesDataToSocket()
        {
            using (var socket = new StubSocket())
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var sender = new SocketPipelineSender(socket, cancellationTokenSource.Token, new ResetHandle()))
            {

                await sender.SendAsync(async m =>
                {
                    foreach (var n in Enumerable.Range(0, 3))
                    {
                        await m.Write((byte)(n + 1));
                    }
                });

                TestExtensions.RunOnBackgroundThread(sender.RunAsync);

                var data = await socket.WaitForData(3);

                cancellationTokenSource.Cancel();

                Assert.Equal(3, data.Length);
                Assert.Equal(1, data[0]);
                Assert.Equal(2, data[1]);
                Assert.Equal(3, data[2]);
            }
        }
    }
}