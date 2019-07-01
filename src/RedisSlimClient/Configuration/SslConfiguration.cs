using System.Net.Security;

namespace RedisSlimClient.Configuration
{
    public sealed class SslConfiguration
    {
        public bool UseSsl { get; set; }

        public string Host { get; }

        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; }
    }
}