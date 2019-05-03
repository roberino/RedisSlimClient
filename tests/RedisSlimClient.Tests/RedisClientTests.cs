using RedisSlimClient.Configuration;
using RedisSlimClient.Io;
using RedisSlimClient.Serialization;
using RedisSlimClient.Tests.Serialization;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RedisSlimClient.Tests
{
    public class RedisClientTests
    {
        [Fact]
        public async Task SetObjectAsync_WritesObjectDataToStream()
        {
            var fakeStream = new FakeNetworkStream();
            var pipeline = new CommandPipeline(fakeStream);
            var client = new RedisClient(new ClientConfiguration("tcp://localhost:2344"), f => new FakeConnection(pipeline));

            var data = new TestDto()
            {
                DataItem1 = "y",
                DataItem2 = DateTime.UtcNow,
                DataItem3 = new AnotherTestDto()
                {
                    DataItem1 = "x"
                }
            };

            fakeStream.Return("OK");

            var ok = await client.SetObjectAsync("x", data);

            var writtenData = fakeStream.GetDataWritten();

            Assert.True(ok);
            Assert.StartsWith("+SET\r\n+x\r\n", writtenData);
        }
    }

    class FakeConnection : IConnection
    {
        private readonly CommandPipeline _commandPipeline;

        public FakeConnection(CommandPipeline commandPipeline)
        {
            _commandPipeline = commandPipeline;
        }

        public Task<ICommandPipeline> ConnectAsync() => Task.FromResult<ICommandPipeline>(_commandPipeline);

        public void Dispose()
        {
        }
    }

    public class FakeNetworkStream : Stream
    {
        public FakeNetworkStream()
        {
            RequestData = new MemoryStream();
            ResponseData = new MemoryStream();
        }

        public MemoryStream RequestData { get; }
        public MemoryStream ResponseData { get; }

        public void Return(string response)
        {
            ResponseData.Write(response);
            ResponseData.Position = 0;
        }

        public string GetDataWritten()
        {
            return Encoding.UTF8.GetString(RequestData.ToArray());
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ResponseData.Read(buffer, offset, count);
        }

        public override void Flush()
        {
            RequestData.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            RequestData.Write(buffer, offset, count);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }
}