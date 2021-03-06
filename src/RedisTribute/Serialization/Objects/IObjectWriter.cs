﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedisTribute.Serialization
{
    public interface IObjectWriter
    {
        void BeginWrite(int itemCount);
        void Raw(byte[] data, int? length = null);
        void WriteNullable<T>(string name, T? data, string onValue) where T : struct;
        void WriteEnum<T>(string name, T data);
        void WriteItem<T>(string name, IEnumerable<T> data);
        void WriteItem<T>(string name, T data);
        void WriteItem(string name, string data);
        void WriteItem(string name, byte[] data);
        void WriteItem(string name, DateTime data);
        void WriteItem(string name, TimeSpan data);
        void WriteItem(string name, short data);
        void WriteItem(string name, int data);
        void WriteItem(string name, long data);
        void WriteItem(string name, char data);
        void WriteItem(string name, bool data);
        void WriteItem(string name, decimal data);
        void WriteItem(string name, double data);
        void WriteItem(string name, float data);
    }
}