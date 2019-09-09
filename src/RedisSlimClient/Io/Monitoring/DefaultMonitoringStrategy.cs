using System;
using System.Threading;

namespace RedisSlimClient.Io.Monitoring
{
    class DefaultMonitoringStrategy : IDisposable
    {
        readonly Timer _timer;

        public DefaultMonitoringStrategy(IRedisDiagnosticClient client, int heartbeatInterval = 1000)
        {
            _timer = new Timer(x => OnHeartbeat((IRedisDiagnosticClient)x), client, heartbeatInterval, heartbeatInterval);
        }

        static void OnHeartbeat(IRedisDiagnosticClient client)
        {
            try
            {
                var results = client.PingAllAsync();
            }
            catch
            {

            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}