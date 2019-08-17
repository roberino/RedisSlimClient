namespace RedisSlimClient.Configuration
{
    public sealed class NetworkConfiguration
    {
        public NetworkConfiguration(IHostAddressResolver dnsResolver = null)
        {
            DnsResolver = dnsResolver ?? new HostAddressResolver();
            PortMappings = new PortMap();
        }

        public PortMap PortMappings { get; }

        public IHostAddressResolver DnsResolver { get; }
    }
}