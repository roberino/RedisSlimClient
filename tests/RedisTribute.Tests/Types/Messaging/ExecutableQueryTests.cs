using RedisTribute.Types.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RedisTribute.UnitTests.Types.Messaging
{
    public class ExecutableQueryTests
    {
        [Fact]
        public void GetBytes_ReturnsData()
        {
            var x = new ExecutableQuery<string>(c => new[] { "x" });

            var data = x.GetBytes();
        }
    }
}
