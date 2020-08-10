using RedisTribute.Configuration;
using RedisTribute.Telemetry;
using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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

        public static ClientConfiguration GetConfiguration(ConfigurationScenario scenario, PipelineMode pipelineMode, Action<string> output = null, int? databaseIndex = null)
        {
            ThreadPool.SetMaxThreads(500, 500);

            var additionalConfig = string.Empty;

            if (scenario.ToString().Contains("Password"))
            {
                additionalConfig = ";password=p@ssw0rd";
            }

            if (databaseIndex.HasValue)
            {
                additionalConfig += $";database={databaseIndex}";
            }

            var config = new ClientConfiguration($"redis://localhost:{(int)scenario}{additionalConfig}")
            {
                PipelineMode = pipelineMode,
                DefaultOperationTimeout = TimeSpan.FromMilliseconds(10000),
                ConnectTimeout = TimeSpan.FromMilliseconds(25000),
                FallbackStrategy = FallbackStrategy.ProactiveRetry,
                HealthCheckInterval = TimeSpan.Zero
            };

            if (output != null)
            {
                config.TelemetrySinks.Add(new TextTelemetryWriter( m =>
                {
                    try
                    {
                        output(m);
                    }
                    catch { }
                }, Severity.Warn | Severity.Error));
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
                config.SslConfiguration.RemoteCertificateValidationCallback =
                    (_, cert, __, err) =>
                    {
                        Debug.WriteLine($"{cert.Subject} {err}");

                        return true;
                    };
            }

            return config;
        }
    }

    public enum ConfigurationScenario
    {
        NonSslBasic = 9096,
        NonSslBasic2 = 9198,
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
