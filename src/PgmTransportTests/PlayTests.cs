using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace PgmTransportTests
{
    [TestFixture]
    public class PlayTests
    {
        private Dictionary<string, Socket> _acceptedSockets = new Dictionary<string,Socket>();
        private Thread _receivingThread;

         [Test]
        public void should_create_socket()
         {
             var ipEndPoint = new IPEndPoint(IPAddress.Any, 2000);
             
             var sendingSocket = new PgmSocket();
             sendingSocket.SendBufferSize = 1024 * 1024;
             sendingSocket.Bind(new IPEndPoint(IPAddress.Any,0));
             SetSendWindow(sendingSocket);
             PgmSocket.EnableGigabit(sendingSocket);
           //  sendingSocket.SetPgmOption(PgmConstants.RM_ADD_RECEIVE_IF,new byte[0]);
             sendingSocket.ApplySocketOptions();
             sendingSocket.Connect(new IPEndPoint(IPAddress.Parse("0.0.0.0"),2000));

             var receivingSocket = new PgmSocket();
             receivingSocket.Bind(ipEndPoint);
             receivingSocket.SetPgmOption(PgmConstants.RM_ADD_RECEIVE_IF, null);
             PgmSocket.EnableGigabit(receivingSocket);
             receivingSocket.ApplySocketOptions();
             receivingSocket.Listen(5);
             var acceptEventArgs = new SocketAsyncEventArgs();
             acceptEventArgs.Completed += OnAccept;

             receivingSocket.AcceptAsync(acceptEventArgs);

             sendingSocket.Send(Encoding.ASCII.GetBytes("toto"));


         }

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var acceptSocket = e.AcceptSocket;
            if((int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error) != 0)
            {
                var receiveEventArgs = new SocketAsyncEventArgs();
                receiveEventArgs.Completed += OnReceive;
                acceptSocket.ReceiveAsync(receiveEventArgs);

            }

            socket.AcceptAsync(e);
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var receivedMessage = e.Buffer;
            Console.WriteLine(string.Format("received buffer : {0}", Encoding.ASCII.GetString(receivedMessage)));
            socket.ReceiveAsync(e);
        }


        private void SetSendWindow(Socket socket)
         {
             var window = new _RM_SEND_WINDOW();
             window.RateKbitsPerSec = 1000;
             window.WindowSizeInMSecs = 3000;
             window.WindowSizeInBytes = 3000;
             byte[] allData = PgmSocket.ConvertStructToBytes(window);
             socket.SetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)1001, allData);
         }
    }
}