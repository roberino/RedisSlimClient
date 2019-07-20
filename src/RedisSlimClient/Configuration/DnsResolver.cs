using System.Collections.Generic;
using System.Net;

namespace RedisSlimClient.Configuration
{
    public sealed class DnsResolver : IDnsResolver
    {
        readonly IDictionary<string, IPHostEntry> _hostLookup;

        public DnsResolver()
        {
            _hostLookup = new Dictionary<string, IPHostEntry>();
        }

        public IDnsResolver Register(IPHostEntry ip)
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

        public IDnsResolver Register(string host, string ip)
        {
            return Register(new IPHostEntry()
            {
                HostName = host,
                Aliases = new string[0],
                AddressList = new[] { IPAddress.Parse(ip) }
            });
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
