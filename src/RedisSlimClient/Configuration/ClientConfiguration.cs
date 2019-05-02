using RedisSlimClient.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Configuration
{
    public class ClientConfiguration : Dictionary<string, string>
    {
        public ClientConfiguration(string connectionString)
        {
            ServerUri = new Uri(connectionString);
            DefaultTimeout = TimeSpan.FromMinutes(1);
        }

        public Uri ServerUri { get; }

        public TimeSpan DefaultTimeout { get; }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public IObjectSerializerFactory SerializerFactory { get; set; } = Serialization.SerializerFactory.Instance;
    }
}