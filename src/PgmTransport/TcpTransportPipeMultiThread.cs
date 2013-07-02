using System;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace PgmTransport
{
    public class TcpTransportPipeMultiThread : TransportPipe
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(TcpTransportPipeMultiThread));


        public TcpTransportPipeMultiThread(int highWaterMark, HighWaterMarkBehavior highWaterMarkBehavior, IPEndPoint endPoint, SendingTransport transport, int sendingThreadNumber = 0)
            : base(highWaterMark, highWaterMarkBehavior, endPoint, transport, sendingThreadNumber)
        {
        }

        public override int MaximumBatchSize
        {
            get { return 1024* 500 ; }
        }

        public override int MaximumBatchCount
        {
            get { return 1024 * 2; }
        }

        public override Socket CreateSocket()
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SendBufferSize = 1024 * 16;
                socket.Connect(EndPoint);
                socket.NoDelay = true;
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