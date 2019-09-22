using System;

namespace RedisTribute.Io
{
    interface IRedirectionInfo
    {
        Uri Location { get; }
        int Slot { get; }
    }
}