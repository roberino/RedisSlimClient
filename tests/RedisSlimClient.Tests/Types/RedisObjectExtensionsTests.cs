using NSubstitute;
using RedisSlimClient.Types;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RedisSlimClient.UnitTests.Types
{
    public class RedisObjectExtensionsTests
    {
        [Fact]
        public void ToObjects_EmptyArray_ReturnsSingleEmptyArrayObject()
        {
            var emptyArray = new[] { new RedisObjectPart
            {
                IsArrayStart = true,
                ArrayIndex = 0,
                Length = 0,
                Level = 0
            }};

            var result = (RedisArray)emptyArray.ToObjects().Single();

            Assert.True(result.IsComplete);
            Assert.Empty(result);
        }

        [Fact]
        public void ToObjects_EmptyArrayFollowedByString_EnumeratesToFirstItemOnly()
        {
            var i1 = new RedisObjectPart
            {
                IsArrayStart = true,
                ArrayIndex = 0,
                Length = 0,
                Level = 0
            };

            var i2 = new RedisObjectPart
            {
                Value = new RedisString()
            };

            var index = -1;

            var enumerable = Substitute.For<IEnumerable<RedisObjectPart>>();

            var enumerator = Substitute.For<IEnumerator<RedisObjectPart>>();

            enumerable.GetEnumerator().Returns(enumerator);

            var items = new[] { i1, i2 };

            enumerator.MoveNext().Returns(call => index++ < 2);
            enumerator.Current.Returns(call => items[index]);

            var result = (RedisArray)enumerable.ToObjects().First();

            Assert.Equal(0, index);
        }
    }
}