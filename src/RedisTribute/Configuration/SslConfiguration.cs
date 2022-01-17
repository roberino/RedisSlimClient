using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RedisTribute.Configuration
{
    public sealed class SslConfiguration
    {
        internal const int DefaultSslPort = 6380;
        internal const int DefaultNonSslPort = 6379;

        string? _certificatePath;
        X509Certificate2? _certificate;

        public bool UseSsl { get; set; }

        public string? SslHost { get; set; }

        public int DefaultPort => UseSsl ? DefaultSslPort : DefaultNonSslPort;

        public X509Certificate2? Certificate
        {
            get => _certificate;
            set
            {
                _certificate = value;

                if (_certificate != null && RemoteCertificateValidationCallback == null)
                {
                    RemoteCertificateValidationCallback = Trust(_certificate);
                }
            }
        }

        public string? CertificatePath
        {
            get => _certificatePath;
            set
            {
                _certificatePath = value;

                if (!string.IsNullOrEmpty(value))
                    Certificate = new X509Certificate2(value);
            }
        }

        public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; set; }

        public LocalCertificateSelectionCallback? ClientCertificateValidationCallback { get; set; }

        static RemoteCertificateValidationCallback Trust(X509Certificate2 issuer)
        {
            return (object _, X509Certificate? certificate, X509Chain? __, SslPolicyErrors sslPolicyError)
                => sslPolicyError == SslPolicyErrors.RemoteCertificateChainErrors
                    && certificate is X509Certificate2 v2
                    && ValidateIssuer(v2, issuer);
        }

        static bool ValidateIssuer(X509Certificate2 certificateToValidate, X509Certificate2 authority)
        {
            var chain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.ExcludeRoot,
                    VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
                    VerificationTime = DateTime.UtcNow,
                    UrlRetrievalTimeout = TimeSpan.FromSeconds(5)
                }
            };

            chain.ChainPolicy.ExtraStore.Add(authority);

            return chain.Build(certificateToValidate);
        }
    }
}