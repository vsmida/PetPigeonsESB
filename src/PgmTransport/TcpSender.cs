using System;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace PgmTransport
{
    public class TcpSender : SocketSender
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TcpSender));

        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 1024;
                socket.Connect(endpoint);
                return socket;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }

        }
    }
}