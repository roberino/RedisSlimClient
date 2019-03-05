using System;
using System.Collections.Generic;

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
    }
}