namespace RedisSlimClient.Configuration
{
    public sealed class NetworkConfiguration
    {
        public NetworkConfiguration(IDnsResolver dnsResolver = null)
        {
            DnsResolver = dnsResolver ?? new DnsResolver();
            PortMappings = new PortMap();
        }

        public PortMap PortMappings { get; }

        public IDnsResolver DnsResolver { get; }
    }
}