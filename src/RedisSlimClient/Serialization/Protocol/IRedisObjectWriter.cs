using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisSlimClient.Serialization.Protocol
{
    interface IRedisObjectWriter
    {
        Task WriteAsync(IEnumerable<object> objects);
    }
}