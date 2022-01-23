using System.Collections.Generic;

namespace RedisTribute.Serialization.CustomSerializers
{
    class KeyValueSerializer<T> : IObjectSerializer<KeyValuePair<string, T>>
    {
        public KeyValuePair<string, T> ReadData(IObjectReader reader, KeyValuePair<string, T> defaultValue)
        {
            var kv = reader.ReadObject(nameof(KeyValueItem<T>), new KeyValueItem<T>());

            return new KeyValuePair<string, T>(kv.Key, kv.Value);
        }

        public void WriteData(KeyValuePair<string, T> instance, IObjectWriter writer)
        {
            writer.WriteItem(nameof(KeyValueItem<T>), new KeyValueItem<T>()
            {
                Key = instance.Key,
                Value = instance.Value
            });
        }
    }

    public class KeyValueItem<T>
    {
        public string Key { get; init; }

        public T Value { get; init; }
    }
}
