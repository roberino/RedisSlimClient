using System.Collections.Generic;

namespace RedisSlimClient.Serialization.CustomSerializers
{
    class DictionarySerializer<T> : IObjectSerializer<IDictionary<string, T>>
    {
        const string ItemName = "_data";
        
        public IDictionary<string, T> ReadData(IObjectReader reader, IDictionary<string, T> defaultValue)
        {
            var items = reader.ReadEnumerable(ItemName, new Dictionary<string, T>());

            return (IDictionary<string, T>)items;
        }

        public void WriteData(IDictionary<string, T> instance, IObjectWriter writer)
        {
            writer.WriteItem(ItemName, (IEnumerable<KeyValuePair<string, T>>)instance);
        }
    }
}
