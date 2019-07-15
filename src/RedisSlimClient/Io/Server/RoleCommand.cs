using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;

namespace RedisSlimClient.Io.Server
{
    class RoleCommand : RedisCommand<IReadOnlyCollection<ServerEndPointInfo>>
    {
        public RoleCommand() : base("ROLE") { }

        protected override IReadOnlyCollection<ServerEndPointInfo> TranslateResult(IRedisObject redisObject)
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

                    foreach(var item in slaveData)
                    {
                        results.Add(new ServerEndPointInfo(slaveData[0].ToString(), int.Parse(slaveData[1].ToString()), ServerRoleType.Slave));
                    }
                }
            }

            return results;
        }
    }
}