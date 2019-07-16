using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisSlimClient.Io.Server
{
    class RoleCommand : RedisCommand<ServerRole>
    {
        public RoleCommand() : base("ROLE") { }

        protected override ServerRole TranslateResult(IRedisObject redisObject)
        {
            // e.g.

            //1) "master"
            //2) (integer)3129659
            //3) 1) 1) "127.0.0.1"
            //      2) "9001"
            //      3) "3129242"
            //   2) 1) "127.0.0.1"
            //      2) "9002"
            //      3) "3129543"

            var results = new List<ServerEndPointInfo>();

            var arr = (RedisArray)redisObject;

            if (arr.Count > 0)
            {
                Enum.TryParse<ServerRoleType>(arr[0].ToString(), true, out var role);

                if (role == ServerRoleType.Master)
                {
                    var slaveData = (RedisArray)arr[2];

                    foreach (var item in slaveData.Cast<RedisArray>())
                    {
                        results.Add(new ServerEndPointInfo(item[0].ToString(), int.Parse(item[1].ToString()), ServerRoleType.Slave));
                    }

                    return new ServerRole(role, null, results);
                }

                if (role == ServerRoleType.Slave)
                {
                    var masterHost = arr[1].ToString();
                    var masterPort = (int)arr[2].ToLong();

                    return new ServerRole(role, new ServerEndPointInfo(masterHost, masterPort, ServerRoleType.Master), results);
                }
            }

            return new ServerRole(ServerRoleType.Unknown, null, results);
        }
    }
}