using System;
using System.Collections.Generic;
using System.Text;

namespace RedisSlimClient.Io.Server
{
    class ServerNodeInfo
    {
        public ServerRoleType RoleType { get; }
        public string Host { get; }
        public int Port { get; }
    }
}