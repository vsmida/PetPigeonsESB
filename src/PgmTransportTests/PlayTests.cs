using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace PgmTransportTests
{
    [TestFixture]
    public class PlayTests
    {
        private Dictionary<string, Socket> _acceptedSockets = new Dictionary<string, Socket>();
        private Thread _receivingThread;
        private PgmSocket _acceptSocket;

        [Test]
        public void should_create_socket()
        {
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.24"), 2000);

            _acceptSocket = new PgmSocket();
            _acceptSocket.Bind(ipEndPoint);
            _acceptSocket.ApplySocketOptions();
            PgmSocket.EnableGigabit(_acceptSocket);
            _acceptSocket.Listen(5);
            var acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += OnAccept;
            
            _acceptSocket.AcceptAsync(acceptEventArgs);


            var sendingSocket = new PgmSocket();
            sendingSocket.SendBufferSize = 1024 * 1024;
            sendingSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            SetSendWindow(sendingSocket);
            PgmSocket.EnableGigabit(sendingSocket);
            sendingSocket.ApplySocketOptions();
            sendingSocket.Connect(ipEndPoint);


            var buffer = Encoding.ASCII.GetBytes("toto");
            var buffer2 = Encoding.ASCII.GetBytes("toto2");
            sendingSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            sendingSocket.Send(buffer2, 0, buffer2.Length, SocketFlags.None);
            var bigBuffer = new byte[200000];
            sendingSocket.Send(bigBuffer, 0, bigBuffer.Length, SocketFlags.None);
            
            Thread.Sleep(1000);
        }

        private void OnAccept(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var acceptSocket = e.AcceptSocket;
            if ((int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error) == 0)
            {
                var receiveEventArgs = new SocketAsyncEventArgs();
                receiveEventArgs.Completed += OnReceive;
        //        _acceptedSockets.Add("toto",acceptSocket);
                byte[] buffer = new byte[1024];
                receiveEventArgs.SetBuffer(buffer, 0, buffer.Length);
                acceptSocket.ReceiveAsync(receiveEventArgs);

            }


            e.AcceptSocket = null;
            socket.AcceptAsync(e);
        }

        private void OnReceive(object sender, SocketAsyncEventArgs e)
        {
            var socket = sender as Socket;
            var receivedMessage = e.Buffer;
            Console.WriteLine(string.Format("received buffer : {0}", Encoding.ASCII.GetString(receivedMessage)));
            socket.ReceiveAsync(e);
        }


        private unsafe void SetSendWindow(Socket socket)
        {
            var window = new _RM_SEND_WINDOW();
            window.RateKbitsPerSec = 1024;
            window.WindowSizeInMSecs = 0;
            window.WindowSizeInBytes = 10000000 * 2;
            byte[] allData = PgmSocket.ConvertStructToBytes(window);
            socket.SetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)1001, allData);
        }


        public unsafe _RM_SEND_WINDOW GetSendWindow(Socket socket)
        {
            int size = sizeof(_RM_SEND_WINDOW);
            byte[] data = socket.GetSocketOption(PgmSocket.PGM_LEVEL, (SocketOptionName)1001, size);
            fixed (byte* pBytes = &data[0])
            {
                return *((_RM_SEND_WINDOW*)pBytes);
            }
        }
    }
}