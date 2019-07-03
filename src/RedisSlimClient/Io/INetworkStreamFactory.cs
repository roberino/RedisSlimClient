using System;
using System.IO;
using System.Threading.Tasks;

namespace RedisSlimClient.Io
{
    interface INetworkStreamFactory : IDisposable
    {
        Task<Stream> CreateStreamAsync();
    }
}