using System;
using System.Collections.Generic;

namespace RedisSlimClient.Configuration
{
    public class ClientConfiguration : Dictionary<string, string>
    {
        public ClientConfiguration(string connectionString)
        {
            ServerUri = new Uri(connectionString);
        }

        public Uri ServerUri { get; }
    }
}