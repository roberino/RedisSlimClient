using System.Collections.Generic;

namespace RedisSlimClient.Serialization
{
    public interface IObjectSerializer
    {
        IEnumerable<IObjectPart> Serialize<T>(T obj);

        T Deserialize<T>(IEnumerable<IObjectPart> parts);
    }
}