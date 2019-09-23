namespace RedisTribute.Io.Monitoring
{
    readonly struct PipelineMetrics
    {
        public PipelineMetrics(int pendingCommands, int pendingReads)
        {
            PendingCommands = pendingCommands;
            PendingReads = pendingReads;
        }

        public int PendingCommands { get; }
        public int PendingReads { get; }

        public float Workload => (PendingReads + PendingCommands);
    }
}