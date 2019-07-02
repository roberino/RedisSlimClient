using System.Net.Security;

namespace RedisSlimClient.Configuration
{
    public sealed class SslConfiguration
    {
        public bool UseSsl { get; set; }

        public string SslHost { get; set; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; }
    }
}