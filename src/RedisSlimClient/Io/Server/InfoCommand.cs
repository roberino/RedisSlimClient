using RedisSlimClient.Io.Commands;
using RedisSlimClient.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace RedisSlimClient.Io.Server
{
    class InfoCommand : RedisCommand<IDictionary<string, IDictionary<string, object>>>
    {
        public InfoCommand() : base("INFO") { }

        protected override IDictionary<string, IDictionary<string, object>> TranslateResult(IRedisObject redisObject)
        {
            var results = new Dictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

            IDictionary<string, object> currentValues = null;

            var reader = new StringReader(redisObject.ToString());

            while (true)
            {
                var line = reader.ReadLine();

                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("#"))
                {
                    currentValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                    results[line.Substring(1).Trim()] = currentValues;

                    continue;
                }

                if (currentValues == null)
                {
                    continue;
                }

                var i = line.IndexOf(':');

                if (i > -1)
                {
                    var key = line.Substring(0, i);
                    var strValue = line.Substring(i + 1);

                    currentValues[key] = long.TryParse(strValue, out var x) ? (object)x : strValue;
                }
            }


            return results;
        }
    }
}