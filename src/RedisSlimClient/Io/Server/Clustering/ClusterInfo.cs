namespace RedisSlimClient.Io.Clustering
{
    class ClusterInfo
    {
        public ClusterInfo(string host, int port, SlotRange[] slots)
        {
            Host = host;
            Port = port;
            Slots = slots;
        }

        public string Host { get; }
        public int Port { get; }
        public SlotRange[] Slots { get; }
    }
}