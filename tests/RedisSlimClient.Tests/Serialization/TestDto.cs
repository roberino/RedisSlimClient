using System;

namespace RedisSlimClient.Tests.Serialization
{
    public class TestDto
    {
        public string DataItem1 { get; set; }

        public DateTime DataItem2 { get; set; }

        public AnotherTestDto DataItem3 { get; set; }
    }

    public class TestDtoWithCollection
    {
        public AnotherTestDto[] DataItems { get; set; }
    }

    public class OtherTestDto
    {
        public int DataItem1 { get; set; }
    }

    public class AnotherTestDto
    {
        public string DataItem1 { get; set; }
    }
}
