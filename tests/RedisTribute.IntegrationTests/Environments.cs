using RedisTribute.Configuration;
using RedisTribute.Telemetry;
using System;

namespace RedisTribute.IntegrationTests
{
    static class Environments
    {
        public static ClientConfiguration DefaultConfiguration(Action<string> output = null, Action<ClientConfiguration> onConfigure = null)
        {
            var conf = GetConfiguration(ConfigurationScenario.NonSslBasic, PipelineMode.Default, output);

            onConfigure?.Invoke(conf);

            return conf;
        }

        public static ClientConfiguration GetAzureConfig()
        {
            var pwd = Environment.GetEnvironmentVariable("AZ_PASSWORD");
            var host = Environment.GetEnvironmentVariable("AZ_HOST");

            if (!string.IsNullOrEmpty(host))
            {
                return new ClientConfiguration($"{host}.redis.cache.windows.net:6380,password={pwd},ssl=True,abortConnect=False");
            }

            return null;
        }

        public static ClientConfiguration GetConfiguration(ConfigurationScenario scenario, PipelineMode pipelineMode, Action<string> output = null)
        {
            var config = new ClientConfiguration($"redis://localhost:{(int)scenario}")
            {
                PipelineMode = pipelineMode,
                DefaultOperationTimeout = TimeSpan.FromMilliseconds(10000),
                ConnectTimeout = TimeSpan.FromMilliseconds(15000),
                FallbackStrategy = FallbackStrategy.ProactiveRetry
            };

            if (output != null)
            {
                config.TelemetryWriter = new TextTelemetryWriter(output, Severity.All);
            }

            config.NetworkConfiguration.PortMappings
                .Map(6376, (int)ConfigurationScenario.NonSslReplicaSetMaster)
                .Map(6377, (int)ConfigurationScenario.NonSslReplicaSetSlave1)
                .Map(6378, (int)ConfigurationScenario.NonSslReplicaSetSlave2);

            config.NetworkConfiguration.DnsResolver
                .Register("redis-master1", "127.0.0.1")
                .Register("redis-slave1", "127.0.0.1")
                .Register("redis-slave2", "127.0.0.1")
                .Map("192.168.0.0/16", "127.0.0.1")
                .Map("172.16.0.0/12", "127.0.0.1");

            if (!scenario.ToString().Contains("NonSsl"))
            {
                config.SslConfiguration.UseSsl = true;
                config.SslConfiguration.CertificatePath = "ca.pem";
            }

            if (scenario.ToString().Contains("Password"))
            {
                config.Password = "p@ssw0rd";
            }

            return config;
        }
    }

    public enum ConfigurationScenario
    {
        NonSslBasic = 9096,
        NonSslWithPassword = 9296,
        SslBasic = 6380,
        NonSslReplicaSetMaster = 9196,
        NonSslReplicaSetSlave1 = 9194,
        NonSslReplicaSetSlave2 = 9195,
        NonSslClusterSet = 7000,
        NonSslUncontactableServer = 9667,
        SslUncontactableServer = 9666
    }
}
