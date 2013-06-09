using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Shared
{
    public static class NetworkUtils
    {
        public static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static IPAddress GetOwnIp()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Cannot find IP");
        }
 
        public static string GetOwnIpString()
        {
            return GetOwnIp().ToString();
            throw new Exception("Cannot find IP");
        }
    }
}