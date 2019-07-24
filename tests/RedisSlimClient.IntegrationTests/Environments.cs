using RedisSlimClient.Configuration;
using RedisSlimClient.Telemetry;
using System;

namespace RedisSlimClient.IntegrationTests
{
    static class Environments
    {
        public static Uri DefaultEndpoint => new Uri($"redis://localhost:{(int)ConfigurationScenario.NonSslBasic}");

        public static ClientConfiguration DefaultConfiguration(Action<string> output = null, Action < ClientConfiguration> onConfigure = null)
        {
            var conf = GetConfiguration(ConfigurationScenario.NonSslBasic, PipelineMode.Default, output);

            onConfigure?.Invoke(conf);

            return conf;
        }

        public static ClientConfiguration GetConfiguration(ConfigurationScenario scenario, PipelineMode pipelineMode, Action<string> output = null)
        {
            var config = new ClientConfiguration($"redis://localhost:{(int)scenario}")
            {
                PipelineMode = pipelineMode,
                DefaultOperationTimeout = TimeSpan.FromMilliseconds(1500),
                ConnectTimeout = TimeSpan.FromMilliseconds(1500)
            };

            if (output != null)
            {
                config.TelemetryWriter = new TextTelemetryWriter(output, Severity.Info);
            }

            config.NetworkConfiguration.PortMappings
                .Map(6376, (int)ConfigurationScenario.NonSslReplicaSetMaster)
                .Map(6377, (int)ConfigurationScenario.NonSslReplicaSetSlave1)
                .Map(6378, (int)ConfigurationScenario.NonSslReplicaSetSlave2);

            config.NetworkConfiguration.DnsResolver
                .Register("redis-master1", "127.0.0.1")
                .Register("redis-slave1", "127.0.0.1")
                .Register("redis-slave2", "127.0.0.1");

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
        NonSslBasic = 9096,
        SslBasic = 6380,
        NonSslReplicaSetMaster = 9196,
        NonSslReplicaSetSlave1 = 9194,
        NonSslReplicaSetSlave2 = 9195,
        NonSslClusterSet = 7000,
        NonSslUncontactableServer = 9667,
        SslUncontactableServer = 9666
    }
}
