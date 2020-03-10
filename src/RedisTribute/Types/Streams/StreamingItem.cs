namespace RedisTribute.Types.Streams
{
    public readonly struct StreamingItem<T>
    {
        public StreamingItem(StreamEntryId id, T data, string hash)
        {
            Id = id;
            Data = data;
            Hash = hash;
        }

        public StreamEntryId Id { get; }
        public string Hash { get; }
        public T Data { get; }
    }
}
