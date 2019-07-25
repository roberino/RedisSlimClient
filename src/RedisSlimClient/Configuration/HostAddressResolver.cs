using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace RedisSlimClient.Configuration
{
    public sealed class HostAddressResolver : IHostAddressResolver
    {
        readonly IDictionary<string, IPHostEntry> _hostLookup;
        readonly IList<(IpSubnet subnet, IPAddress address)> _ipMapping;

        public HostAddressResolver()
        {
            _hostLookup = new Dictionary<string, IPHostEntry>();
            _ipMapping = new List<(IpSubnet subnet, IPAddress address)>();
        }

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
    }
}
