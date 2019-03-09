using System.Collections.Generic;

namespace RedisSlimClient.Serialization
{
    public interface IObjectGraphExporter
    {
        Dictionary<string, object> GetObjectData(object instance);

        void WriteObjectData(object instance, IObjectWriter writer, int level);
    }
}