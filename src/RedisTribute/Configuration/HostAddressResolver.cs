using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace RedisTribute.Configuration
{
    public sealed class HostAddressResolver : IHostAddressResolver
    {
        static readonly IpSubnet[] _privateSubnets;

        readonly IDictionary<string, IPHostEntry> _hostLookup;
        readonly IList<(IpSubnet subnet, IPAddress address)> _ipMapping;

        static HostAddressResolver()
        {
            _privateSubnets = new[]
            {
                new IpSubnet("10.0.0.0/8"),
                new IpSubnet("172.16.0.0/12"),
                new IpSubnet("192.168.0.0/16")
            };
        }

        public HostAddressResolver() : this(new Dictionary<string, IPHostEntry>(), new List<(IpSubnet subnet, IPAddress address)>())
        {
        }

        HostAddressResolver(IDictionary<string, IPHostEntry> hostLookup, IList<(IpSubnet subnet, IPAddress address)> ipMappings)
        {
            _hostLookup = hostLookup;
            _ipMapping = ipMappings;
        }

        public static string LocalHost => nameof(LocalHost).ToLower();

        public static bool IsPrivateNetworkAddress(IPAddress address) => _privateSubnets.Any(s => s.IsAddressOnSubnet(address));
        
        public IHostAddressResolver Register(IPHostEntry ip)
        {
            _hostLookup[ip.HostName] = ip;

            if (ip.Aliases != null)
            {
                foreach (var alias in ip.Aliases)
                {
                    _hostLookup[alias] = ip;
                }
            }

            return this;
        }

        public IHostAddressResolver Register(string host, string ip)
        {
            return Register(new IPHostEntry()
            {
                HostName = host,
                Aliases = new string[0],
                AddressList = new[] { IPAddress.Parse(ip) }
            });
        }

        public IHostAddressResolver Map(string cidrOrIpAddress, string targetIp)
        {
            var subnet = new IpSubnet(cidrOrIpAddress);

            _ipMapping.Add((subnet, IPAddress.Parse(targetIp)));

            return this;
        }

        public IPAddress Resolve(IPAddress ipAddress)
        {
            var match = _ipMapping.FirstOrDefault(m => m.subnet.IsAddressOnSubnet(ipAddress)).address;

            return match ?? ipAddress;
        }

        public IPHostEntry Resolve(string host)
        {
            if (_hostLookup.TryGetValue(host, out var entry))
            {
                return entry;
            }

            return Dns.GetHostEntry(host);
        }

        public IHostAddressResolver Clone()
        {
            return new HostAddressResolver(_hostLookup.ToDictionary(k => k.Key, v => v.Value), _ipMapping.ToList());
        }
    }
}
