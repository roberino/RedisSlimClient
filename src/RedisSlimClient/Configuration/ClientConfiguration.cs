using RedisSlimClient.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Configuration
{
    public class ClientConfiguration : Dictionary<string, string>
    {
        public ClientConfiguration(string connectionOptions)
        {
            ServerUri = new Uri(connectionOptions);
            DefaultTimeout = TimeSpan.FromMinutes(1);
        }

        public Uri ServerUri { get; private set; }

        public TimeSpan DefaultTimeout { get; }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public bool UseAsyncronousPipeline { get; set; } = true;

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