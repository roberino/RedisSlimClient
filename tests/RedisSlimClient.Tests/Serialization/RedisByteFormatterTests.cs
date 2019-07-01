//using RedisSlimClient.Serialization.Protocol;
//using System;
//using System.Text;
//using Xunit;

//namespace RedisSlimClient.UnitTests.Serialization
//{
//    public class RedisByteFormatterTests
//    {
//        readonly Memory<byte> _output;
//        readonly RedisByteFormatter _formatter;

//        public RedisByteFormatterTests()
//        {
//            _output = new Memory<byte>(new byte[128]);
//            _formatter = new RedisByteFormatter(_output);
//        }

//        [Fact]
//        public void Write_SomeStringDataBulk_WritesExpectedBytes()
//        {
//            _formatter.Write("some-data", true);

//            AssertExpectedString("$9\r\nsome-data\r\n");
//        }

//        [Fact]
//        public void Write_Integer_WritesExpectedBytes()
//        {
//            _formatter.Write(6382334);

//            AssertExpectedString(":6382334\r\n");
//        }

//        [Fact]
//        public void Write_Objects_WritesExpectedBytes()
//        {
//            _formatter.Write(new object[] { 6382334, "abc" });

//            AssertExpectedString("*2\r\n:6382334\r\n$3\r\nabc\r\n");
//        }

//        [Fact]
//        public void Write_SomeStringDataSimple_WritesExpectedBytes()
//        {
//            _formatter.Write("some-data");

//            AssertExpectedString("+some-data\r\n");
//        }

//        void AssertExpectedString(string expected)
//        {
//            var bytes = _output.Slice(0, _formatter.CurrentPosition).ToArray();
//            var actual = Encoding.ASCII.GetString(bytes);

//            Assert.Equal(expected, actual);
//        }
//    }
//}