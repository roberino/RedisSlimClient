using System;

namespace RedisSlimClient.Io
{
    interface IRedirectionInfo
    {
        Uri Location { get; }
        int Slot { get; }
    }
}