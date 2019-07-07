using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisSlimClient.Io.Pipelines
{
    interface IManagedSocket : ISocket
    {
        Task<Stream> CreateStream();
        Socket Socket { get; }
    }
}
