using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace RedisSlimClient.Configuration
{
    class IpSubnet
    {
        readonly IPAddress subnetAddress;
        readonly IPAddress ipMask;

        public IpSubnet(string cidr)
        {
            if (!cidr.Contains("/"))
            {
                cidr = $"{cidr}/32";
            }

            Cidr = cidr;

            var delimiterIndex = cidr.IndexOf('/');
            var ipSubnet = cidr.Substring(0, delimiterIndex);
            var mask = cidr.Substring(delimiterIndex + 1);

            subnetAddress = IPAddress.Parse(ipSubnet);

            if (subnetAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // ipv6
                var ip = BigInteger.Parse("00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber) << (128 - int.Parse(mask));

                var maskBytes = new[]
                {
                (byte)((ip & BigInteger.Parse("00FF000000000000000000000000000000", NumberStyles.HexNumber)) >> 120),
                (byte)((ip & BigInteger.Parse("0000FF0000000000000000000000000000", NumberStyles.HexNumber)) >> 112),
                (byte)((ip & BigInteger.Parse("000000FF00000000000000000000000000", NumberStyles.HexNumber)) >> 104),
                (byte)((ip & BigInteger.Parse("00000000FF000000000000000000000000", NumberStyles.HexNumber)) >> 96),
                (byte)((ip & BigInteger.Parse("0000000000FF0000000000000000000000", NumberStyles.HexNumber)) >> 88),
                (byte)((ip & BigInteger.Parse("000000000000FF00000000000000000000", NumberStyles.HexNumber)) >> 80),
                (byte)((ip & BigInteger.Parse("00000000000000FF000000000000000000", NumberStyles.HexNumber)) >> 72),
                (byte)((ip & BigInteger.Parse("0000000000000000FF0000000000000000", NumberStyles.HexNumber)) >> 64),
                (byte)((ip & BigInteger.Parse("000000000000000000FF00000000000000", NumberStyles.HexNumber)) >> 56),
                (byte)((ip & BigInteger.Parse("00000000000000000000FF000000000000", NumberStyles.HexNumber)) >> 48),
                (byte)((ip & BigInteger.Parse("0000000000000000000000FF0000000000", NumberStyles.HexNumber)) >> 40),
                (byte)((ip & BigInteger.Parse("000000000000000000000000FF00000000", NumberStyles.HexNumber)) >> 32),
                (byte)((ip & BigInteger.Parse("00000000000000000000000000FF000000", NumberStyles.HexNumber)) >> 24),
                (byte)((ip & BigInteger.Parse("0000000000000000000000000000FF0000", NumberStyles.HexNumber)) >> 16),
                (byte)((ip & BigInteger.Parse("000000000000000000000000000000FF00", NumberStyles.HexNumber)) >> 8),
                (byte)((ip & BigInteger.Parse("00000000000000000000000000000000FF", NumberStyles.HexNumber)) >> 0),
            };
                ipMask = new IPAddress(maskBytes);
            }
            else
            {
                // ipv4
                var ip = 0xFFFFFFFF << (32 - int.Parse(mask));

                var maskBytes = new[]
                {
                (byte)((ip & 0xFF000000) >> 24),
                (byte)((ip & 0x00FF0000) >> 16),
                (byte)((ip & 0x0000FF00) >> 8),
                (byte)((ip & 0x000000FF) >> 0),
            };

                ipMask = new IPAddress(maskBytes);
            }
        }

        public string Cidr { get; }

        public bool IsAddressOnSubnet(IPAddress address)
        {
            var addressOctets = address.GetAddressBytes();
            var subnetOctets = ipMask.GetAddressBytes();
            var networkOctets = subnetAddress.GetAddressBytes();

            // ensure that IPv4 isn't mixed with IPv6
            if (addressOctets.Length != subnetOctets.Length
                || addressOctets.Length != networkOctets.Length)
            {
                return false;
            }

            for (var i = 0; i < addressOctets.Length; i += 1)
            {
                var addressOctet = addressOctets[i];
                var subnetOctet = subnetOctets[i];
                var networkOctet = networkOctets[i];

                if (networkOctet != (addressOctet & subnetOctet))
                {
                    return false;
                }
            }
            return true;
        }
    }
}