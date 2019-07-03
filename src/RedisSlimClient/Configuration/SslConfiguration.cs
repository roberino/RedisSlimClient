using System.Net.Security;

namespace RedisSlimClient.Configuration
{
    public sealed class SslConfiguration
    {
        internal const int DefaultSslPort = 6380;
        internal const int DefaultNonSslPort = 6379;

        public bool UseSsl { get; set; }

        public string SslHost { get; set; }

        public int DefaultPort => UseSsl ? DefaultSslPort : DefaultNonSslPort;

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; }
    }
}