using RedisSlimClient.Io.Net;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.UnitTests.Io.Net
{
    public class SocketStateTests
    {
        [Fact]
        public void Status_NewState_ReturnsDisconnected()
        {
            var state = new SocketState(() => true);

            Assert.Equal(SocketStatus.Disconnected, state.Status);
        }

        [Fact]
        public async Task DoConnect_Status_ReturnsConnected()
        {
            var state = new SocketState(() => true);

            await state.DoConnect(() => Task.CompletedTask);

            Assert.Equal(SocketStatus.Connected, state.Status);
        }

        [Fact]
        public async Task Id_DoConnect_ReturnsIncrementedId()
        {
            var state = new SocketState(() => true);

            var id = state.Sequence;

            await state.DoConnect(() => Task.CompletedTask);

            var newId = state.Sequence;

            Assert.Equal(id + 1, newId);
        }

        [Fact]
        public async Task Changed_MultipleDoConnectErrors_FiresEachError()
        {
            var state = new SocketState(() => true);

            var raised = 0;

            state.Changed += x =>
            {
                if (x.Status == SocketStatus.ConnectFault)
                    raised++;
            };

            foreach (var n in Enumerable.Range(1, 10))
            {
                try
                {
                    await state.DoConnect(() => throw new SocketException());
                }
                catch { }
            }

            Assert.Equal(10, raised);
        }

        [Fact]
        public void ReadError_Status_ReturnsReadFault()
        {
            var state = new SocketState(() => true);

            state.ReadError(new TimeoutException());

            Assert.Equal(SocketStatus.ReadFault, state.Status);
        }

        [Fact]
        public void Changed_MultipleReadErrors_FiresOnce()
        {
            var state = new SocketState(() => true);

            var status = state.Status;
            var raised = 0;

            state.Changed += (x) =>
            {
                status = x.Status;
                raised++;
            };

            state.ReadError(new TimeoutException());
            state.ReadError(new TimeoutException());
            state.ReadError(new TimeoutException());

            Assert.Equal(1, raised);
            Assert.Equal(SocketStatus.ReadFault, status);
        }
    }
}
