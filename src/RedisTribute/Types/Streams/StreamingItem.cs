using RedisTribute.Serialization;
using System;

namespace RedisTribute.Types.Streams
{
    public readonly struct StreamingItem<T>
    {
        public StreamingItem(StreamEntryId id, T data, string correlationId, string clientId)
        {
            Id = id;
            Data = data;
            CorrelationId = correlationId;
            ClientId = clientId;
            Hash = id.ToBytes().CreateHash();
        }
        
        internal StreamingItem(StreamEntryId id, T data, string hash, string correlationId, string clientId)
        {
            Id = id;
            Data = data;
            CorrelationId = correlationId;
            ClientId = clientId;
            Hash = hash;
        }

        public StreamEntryId Id { get; }
        public string ClientId { get; }
        public string Hash { get; }
        public string CorrelationId { get; }

        public T Data { get; }

        public StreamingItem<T> Next()
        {
            var hash = Convert.FromBase64String(Hash);
            var next = Id.ToBytes();
            var concat = new byte[hash.Length + next.Length];

            Array.Copy(hash, 0, concat, 0, hash.Length);
            Array.Copy(next, 0, concat, hash.Length, next.Length);

            return new StreamingItem<T>(Id, Data, concat.CreateHash(), CorrelationId, ClientId);
        }
    }
}
