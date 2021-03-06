﻿using System;
using System.Collections.Generic;

namespace RedisTribute.UnitTests.Serialization
{
    public class TestComplexDto
    {
        public string DataItem1 { get; set; }

        public DateTime DataItem2 { get; set; }

        public TestDtoWithString DataItem3 { get; set; }
    }

    public class TestDtoWithGenericCollection<T>
    {
        public IList<T> Items { get; set; } = new List<T>();
    }

    public class TestDtoWithCollection
    {
        public TestDtoWithString[] DataItems { get; set; }
    }

    public class TestDtoWithInt
    {
        public int DataItem1 { get; set; }
    }

    public class TestDtoWithString
    {
        public string DataItem1 { get; set; }
    }

    public class TestDtoWithDouble
    {
        public double DataItem1 { get; set; }

        public string DataItem2 { get; set; }
    }

    public class TestDtoWithEnum
    {
        public TestEnum DataItem1 { get; set; }
    }

    public class TestDtoWithTimeSpan
    {
        public TimeSpan Time1 { get; set; }
    }

    public class TestDtoWithGeneric<T>
    {
        public T DataItem1 { get; set; }
    }

    public enum TestEnum
    {
        None,
        Value1,
        Value2
    }

    [Flags]
    public enum TestFlagEnum : byte
    {
        None = 0,
        Value1 = 1,
        Value2 = 2,
        Value3 = 4
    }
}
