namespace RedisSlimClient.Io.Monitoring
{
    readonly struct PipelineMetrics
    {
        public PipelineMetrics(int pendingWrites, int pendingReads)
        {
            PendingWrites = pendingWrites;
            PendingReads = pendingReads;
        }

        public int PendingWrites { get; }
        public int PendingReads { get; }

        public float Workload => (PendingReads + PendingWrites);
    }
}