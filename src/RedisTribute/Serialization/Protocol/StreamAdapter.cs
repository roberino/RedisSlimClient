using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RedisTribute.Serialization.Protocol
{
    class StreamAdapter : IRedisObjectWriter
    {
        readonly Stream _stream;

        public StreamAdapter(Stream stream)
        {
            _stream = stream;
        }

        public Task WriteAsync(IEnumerable<object> objects)
        {
            _stream.Write(objects.ToArray());
            return _stream.FlushAsync();
        }
    }
}