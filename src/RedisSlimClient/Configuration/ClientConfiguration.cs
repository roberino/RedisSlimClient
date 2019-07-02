using RedisSlimClient.Serialization;
using RedisSlimClient.Telemetry;
using System;
using System.Text;

namespace RedisSlimClient.Configuration
{
    public class ClientConfiguration : IReadWriteBufferSettings, ISerializerSettings
    {
        public ClientConfiguration(string connectionOptions)
        {
            SslConfiguration = new SslConfiguration();
            Parse(connectionOptions);
        }

        public SslConfiguration SslConfiguration { get; }

        public Uri ServerUri { get; private set; }

        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public PipelineMode PipelineMode { get; set; } = PipelineMode.Default;

        public int ConnectionPoolSize { get; set; } = 1;

        public int ReadBufferSize { get; set; } = 1024;

        public int WriteBufferSize { get; set; } = 1024;

        public IObjectSerializerFactory SerializerFactory { get; set; } = Serialization.SerializerFactory.Instance;

        public ITelemetryWriter TelemetryWriter { get; set; }

        void Parse(string connectionOptions)
        {
            foreach(var item in connectionOptions.Split(','))
            {
                var kv = item.Split('=');

                if (kv.Length == 1)
                {
                    ServerUri = ParseUri(kv[0]);
                }
                else
                {
                    if (kv.Length == 2)
                    {
                        switch (kv[0])
                        {
                            case nameof(Encoding):
                                Encoding = Encoding.GetEncoding(kv[1]);
                                break;
                            case nameof(DefaultTimeout):
                                DefaultTimeout = TimeSpan.Parse(kv[1]);
                                break;
                            case nameof(ConnectionPoolSize):
                                ConnectionPoolSize = int.Parse(kv[0]);
                                break;
                            case nameof(ReadBufferSize):
                                ReadBufferSize = int.Parse(kv[0]);
                                break;
                            case nameof(WriteBufferSize):
                                WriteBufferSize = int.Parse(kv[0]);
                                break;
                            case nameof(SslConfiguration.SslHost):
                                SslConfiguration.SslHost = kv[0];
                                break;
                            case nameof(SslConfiguration.UseSsl):
                                SslConfiguration.UseSsl = bool.Parse(kv[0].ToLower());
                                break;
                        }
                    }
                }
            }
        }

        Uri ParseUri(string uri)
        {
            const string protocol = "tcp://";

            if (uri.StartsWith(protocol))
            {
                return new Uri(uri);
            }

            return new Uri($"{protocol}{uri}");
        }
    }
}