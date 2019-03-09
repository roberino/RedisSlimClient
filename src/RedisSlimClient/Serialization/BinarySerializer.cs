using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Serialization
{
    class BinarySerializer : IObjectSerializer
    {
        public IEnumerable<IObjectPart> Serialize<T>(T obj)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(IEnumerable<IObjectPart> parts)
        {
            throw new NotImplementedException();
        }
    }
}