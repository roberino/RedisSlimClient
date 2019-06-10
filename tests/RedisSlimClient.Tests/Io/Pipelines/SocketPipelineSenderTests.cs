using NSubstitute;
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
            var socket = Substitute.For<ISocket>();
            var cancellationTokenSource = new CancellationTokenSource();
            var sender = new SocketPipelineSender(socket, cancellationTokenSource.Token);

            socket.SendAsync(Arg.Any<ReadOnlySequence<byte>>()).Returns(call => (int)call.Arg<ReadOnlySequence<byte>>().Length);

            await sender.SendAsync(m =>
            {
                foreach (var n in Enumerable.Range(0, 5))
                {
                    m.Span[n] = (byte)(n + 1);
                }
                return 5;
            });

            var handle = new ManualResetEvent(false);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                sender.RunAsync().Wait();
                handle.Set();
            });

            handle.WaitOne();

            await socket.Received().SendAsync(Arg.Is<ReadOnlySequence<byte>>(x => x.Length == 5));
        }
    }
}