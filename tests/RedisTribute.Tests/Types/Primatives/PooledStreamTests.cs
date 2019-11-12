using RedisTribute.Types.Primatives;
using System;
using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace RedisTribute.UnitTests.Types.Primatives
{
    public class PooledStreamTests
    {
        [Fact]
        public void GetStream_SomeLength_ReturnsCorrectSizedStreamAtPositionZero()
        {
            var streamPool = StreamPool.Instance;

            using (var stream = streamPool.CreateWritable(10))
            {
                Assert.Equal(0, stream.Position);
                Assert.Equal(0, stream.Length);
            }
        }

        [Fact]
        public async Task WriteAsync_WritableStream_ReturnsCorrectLength()
        {
            var streamPool = StreamPool.Instance;

            using (var stream = streamPool.CreateWritable(10))
            {
                await stream.WriteAsync(new byte[] { 1, 2, 3 });

                Assert.Equal(3, stream.Length);
            }
        }

        [Fact]
        public void CopyFrom_SingleSpanReadOnlySequence_ReturnsCopyOfDataInStream()
        {
            var streamPool = StreamPool.Instance;

            var data = new ReadOnlySequence<byte>(new byte[] { 1, 3, 5 });

            using(var stream = streamPool.CreateReadOnlyCopy(data))
            {
                Assert.Equal(0, stream.Position);
                Assert.Equal(3, stream.Length);
                Assert.Equal(1, stream.ReadByte());
                Assert.Equal(3, stream.ReadByte());
                Assert.Equal(5, stream.ReadByte());
            }
        }

        [Fact]
        public void Write_CopiedStream_ThrowsNotSupportedException()
        {
            var streamPool = StreamPool.Instance;

            var data = new ReadOnlySequence<byte>(new byte[] { 1, 3, 5 });

            using (var stream = streamPool.CreateReadOnlyCopy(data))
            {
                bool exceptionThrown = false;
                try
                {
                    stream.Write(new byte[6]);
                }
                catch (NotSupportedException)
                {
                    exceptionThrown = true;
                }
                Assert.True(exceptionThrown);
            }
        }

        [Fact]
        public void CopyFrom_MultiSpanReadOnlySequence_ReturnsCopyOfDataInStream()
        {
            var streamPool = StreamPool.Instance;


            var segStart = new Segment(1, 3, 5);
            var segEnd = segStart.Chain(7, 11, 17);
            var data = new ReadOnlySequence<byte>(segStart, 0, segEnd, 3);

            using (var stream = streamPool.CreateReadOnlyCopy(data))
            {
                Assert.Equal(0, stream.Position);
                Assert.Equal(6, stream.Length);
                Assert.Equal(1, stream.ReadByte());
                Assert.Equal(3, stream.ReadByte());
                Assert.Equal(5, stream.ReadByte());
                Assert.Equal(7, stream.ReadByte());
                Assert.Equal(11, stream.ReadByte());
                Assert.Equal(17, stream.ReadByte());
            }
        }

        class Segment: ReadOnlySequenceSegment<byte>
        {
            public Segment(params byte[] data)
            {
                Memory = new ReadOnlyMemory<byte>(data);
                RunningIndex = 0;
            }

            public Segment Chain(params byte[] data)
            {
                var seg = new Segment(data);
                seg.RunningIndex = Memory.Length;
                Next = seg;
                return seg;
            }
        }
    }
}