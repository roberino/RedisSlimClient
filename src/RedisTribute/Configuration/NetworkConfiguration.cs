namespace RedisTribute.Configuration
{
    public sealed class NetworkConfiguration
    {
        public NetworkConfiguration(IHostAddressResolver? dnsResolver = null) : this(dnsResolver ?? new HostAddressResolver(), new PortMap())
        {
        }

        NetworkConfiguration(IHostAddressResolver dnsResolver, PortMap portMappings)
        {
            DnsResolver = dnsResolver;
            PortMappings = portMappings;
        }

        public PortMap PortMappings { get; }

        public IHostAddressResolver DnsResolver { get; }

        internal NetworkConfiguration Clone()
        {
            return new NetworkConfiguration(DnsResolver.Clone(), PortMappings.Clone());
        }
    }
}