using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RedisSlimClient.Io.Net
{
    public class PortUtility
    {
        public static int[] GetFreePorts(int numberOfPortsRequired)
        {
            var openListeners = new List<TcpListener>(numberOfPortsRequired);
            var ports = new int[numberOfPortsRequired];

            try
            {
                for (var i = 0; i < numberOfPortsRequired; i++)
                {
                    var listener = new TcpListener(IPAddress.Loopback, 0);
                    listener.Start();
                    openListeners.Add(listener);
                    ports[i] = ((IPEndPoint)listener.LocalEndpoint).Port;
                }
            }
            finally
            {
                foreach (var openListener in openListeners)
                {
                    openListener.Stop();
                }
            }

            return ports;
        }
    }
}