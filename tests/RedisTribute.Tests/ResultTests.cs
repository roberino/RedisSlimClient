using Xunit;

namespace RedisTribute.UnitTests
{
    public class ResultTests
    {
        [Fact]
        public void FoundResult_IfFoundHandler_CallsHandler()
        {
            var result = Result<string>.Found("x");
            var found = false;

            var value = (string)result
                .IfFound(v => { found = true; })
                .IfNotFound(() => "y");

            Assert.True(found);
            Assert.Equal("x", value);
        }

        [Fact]
        public void FoundResult_IfNotFoundHandlerFollowedByFound_CallsHandler()
        {
            var result = Result<string>.Found("x");
            var found = false;

            var value = (string)result
                .IfNotFound(() => "y")
                .IfFound(v => { found = true; })
                .IfFound(v => v + "z");

            Assert.True(found);
            Assert.Equal("xz", value);
        }

        [Fact]
        public void FoundResult_IfFoundTransform_ReturnsTransformedValue()
        {
            var result = Result<string>.Found("x");
            var found = false;

            var value = (byte)result
                .IfFound(v => { found = true; })
                .IfFound(v => (byte)v[0])
                .IfNotFound(() => (byte)'y');

            Assert.True(found);
            Assert.Equal((byte)'x', value);
        }

        [Fact]
        public void NotFoundResult_IfNotFoundTransform_ReturnsTransformedValue()
        {
            var result = Result<string>.NotFound();
            var found = false;

            var value = (string)result
                .IfFound(v => found = true)
                .IfNotFound(() => "y");

            Assert.False(found);
            Assert.Equal("y", value);
        }

        [Fact]
        public void CancelledResult_IfNotFoundTransform_ReturnsTransformedValue()
        {
            var result = Result<string>.Cancelled();
            var cancelled = false;

            var value = (string)result
                .IfNotFound(() => "y")
                .IfCancelled(() => { cancelled = true; })
                .IfCancelled(() => "z");

            Assert.True(cancelled);
            Assert.Equal("z", value);
        }
    }
}
