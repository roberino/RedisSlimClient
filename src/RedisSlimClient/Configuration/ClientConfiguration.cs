using RedisSlimClient.Serialization;
using System;
using System.Text;

namespace RedisSlimClient.Configuration
{
    public class ClientConfiguration
    {
        public ClientConfiguration(string connectionOptions)
        {
            Parse(connectionOptions);
        }

        public Uri ServerUri { get; private set; }

        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public bool UseAsyncronousPipeline { get; set; } = true;

        public int ConnectionPoolSize { get; set; } = 1;

        public IObjectSerializerFactory SerializerFactory { get; set; } = Serialization.SerializerFactory.Instance;

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