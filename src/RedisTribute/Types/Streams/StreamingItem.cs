using RedisTribute.Serialization;
using System;

namespace RedisTribute.Types.Streams
{
    public readonly struct StreamingItem<T>
    {
        public StreamingItem(StreamEntryId id, T data)
        {
            Id = id;
            Data = data;
            Hash = id.ToBytes().CreateHash();
        }
        
        internal StreamingItem(StreamEntryId id, T data, string hash)
        {
            Id = id;
            Data = data;
            Hash = hash;
        }

        public StreamEntryId Id { get; }
        public string Hash { get; }
        public T Data { get; }

        public StreamingItem<T> Next(StreamEntryId id, T data)
        {
            var hash = Convert.FromBase64String(Hash);
            var next = id.ToBytes();
            var concat = new byte[hash.Length + next.Length];

            Array.Copy(hash, 0, concat, 0, hash.Length);
            Array.Copy(next, 0, concat, hash.Length, next.Length);

            return new StreamingItem<T>(id, data, concat.CreateHash());
        }
    }
}
