using RedisTribute.Configuration;
using RedisTribute.Serialization;
using System;
using Xunit;

namespace RedisTribute.UnitTests.Serialization
{
    public class SerializationExtensionsTests
    {
        [Fact]
        public void Serialize_LargeObjectWithBinaryData_ReturnsValidBytes()
        {
            var config = new ClientConfiguration("localhost");

            var rnd = new Random();

            var bytes = new byte[8096];

            rnd.NextBytes(bytes);

            var data = config.Serialize((x: "abc", y: bytes));
        }
    }
}
