using RedisSlimClient.Io.Net;
using System;
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

            var id = state.Id;

            await state.DoConnect(() => Task.CompletedTask);

            var newId = state.Id;

            Assert.Equal(id + 1, newId);
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
