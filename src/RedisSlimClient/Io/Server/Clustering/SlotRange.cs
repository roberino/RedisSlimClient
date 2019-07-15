namespace RedisSlimClient.Io.Server.Clustering
{
    readonly struct SlotRange
    {
        public SlotRange(long start, long end) : this()
        {
            Start = start;
            End = end;
        }

        public long Start { get; }

        public long End { get; }

        public bool IsWithinRange(long hash)
        {
            return hash >= Start && hash <= End;
        }
    }
}