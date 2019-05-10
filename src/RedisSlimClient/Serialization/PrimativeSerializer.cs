using System;

namespace RedisSlimClient.Serialization
{
    class PrimativeSerializer<T> : IObjectSerializer<T>
    {
        readonly Func<byte[], T> _bytesToItem;
        readonly Func<T, byte[]> _itemToBytes;

        public PrimativeSerializer(Func<byte[], T> bytesToItem, Func<T, byte[]> itemToBytes)
        {
            _bytesToItem = bytesToItem;
            _itemToBytes = itemToBytes;
        }

        public T ReadData(IObjectReader reader, T defaulValue) => _bytesToItem(reader.ReadRaw());

        public void WriteData(T instance, IObjectWriter writer) => writer.WriteRaw(_itemToBytes(instance));
    }
}