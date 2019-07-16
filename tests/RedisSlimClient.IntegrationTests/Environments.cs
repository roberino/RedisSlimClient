using RedisSlimClient.Configuration;
using System;
using System.Collections.Generic;

namespace RedisSlimClient.IntegrationTests
{
    static class Environments
    {
        static readonly IDictionary<ConfigurationScenario, Uri> _configuredEnvironments;

        static Environments()
        {
            _configuredEnvironments = new Dictionary<ConfigurationScenario, Uri>()
            {
                [ConfigurationScenario.NonSslBasic] = new Uri("redis://localhost:9096/"),
                [ConfigurationScenario.SslBasic] = new Uri("redis://localhost:6380/"),
                [ConfigurationScenario.NonSslReplicaSet] = new Uri("redis://localhost:9196/")
            };
        }
        public static Uri DefaultEndpoint => _configuredEnvironments[ConfigurationScenario.NonSslBasic];

        public static ClientConfiguration GetConfiguration(ConfigurationScenario scenario, PipelineMode pipelineMode)
        {
            var url = _configuredEnvironments[scenario];

            var config = new ClientConfiguration(url.ToString())
            {
                PipelineMode = pipelineMode
            };

            if (!scenario.ToString().Contains("NonSsl"))
            {
                config.SslConfiguration.UseSsl = true;
                config.SslConfiguration.CertificatePath = "ca.pem";
            }

            return config;
        }
    }

    public enum ConfigurationScenario
    {
        NonSslBasic,
        SslBasic,
        NonSslReplicaSet
    }
}
