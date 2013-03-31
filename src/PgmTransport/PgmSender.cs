using System;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace PgmTransport
{
    public class PgmSender : SocketSender
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(PgmSender));

        protected override Socket CreateSocket(IPEndPoint endpoint)
        {
            try
            {
                var sendingSocket = new PgmSocket();
                sendingSocket.SendBufferSize = 1024 * 1024;
                sendingSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                sendingSocket.SetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)PgmConstants.RM_SEND_WINDOW_ADV_RATE, 20);
                var window = new _RM_SEND_WINDOW();
                window.RateKbitsPerSec = 0;
                window.WindowSizeInBytes = 100 * 1024 * 1024;
                window.WindowSizeInMSecs = 50;
                sendingSocket.SetSendWindow(window);

                sendingSocket.EnableGigabit();
                var tt2 = sendingSocket.GetSendWindow();
                _logger.Info(string.Format("connecting socket to {0}", endpoint));
                sendingSocket.Connect(endpoint);
                _logger.Info(string.Format("finished connecting socket to {0}", endpoint));

                return sendingSocket;
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return null;
            }

        }

    }
}