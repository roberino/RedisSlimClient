using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisTribute.Serialization.Protocol
{
    interface IRedisObjectWriter
    {
        Task WriteAsync(IEnumerable<object> objects);
    }
}