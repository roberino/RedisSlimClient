using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisTribute.Io.Net
{
    interface IManagedSocket : ISocket
    {
        Task<Stream> CreateStream();
        Socket Socket { get; }
    }
}
