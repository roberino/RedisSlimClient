using System.Collections.Generic;

namespace RedisTribute.Serialization.CustomSerializers
{
    class DictionarySerializer<T> : IObjectSerializer<Dictionary<string, T>>
    {
        const string ItemName = "_data";
        
        public Dictionary<string, T> ReadData(IObjectReader reader, Dictionary<string, T> defaultValue)
        {
            var items = reader.ReadEnumerable(ItemName, new Dictionary<string, T>());

            return (Dictionary<string, T>)items;
        }

        public void WriteData(Dictionary<string, T> instance, IObjectWriter writer)
        {
            writer.WriteItem(ItemName, (IEnumerable<KeyValuePair<string, T>>)instance);
        }
    }
}
