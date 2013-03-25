using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;

namespace PgmTransportTests
{
    [TestFixture]
    public class SocketTests
    {
         [Test]
        public void how_to_shutdown_gracefully_without_loss_synchronous()
         {
             var endpoint = new IPEndPoint(IPAddress.Loopback, 2034);

             var bindSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
             bindSocket.Bind(endpoint);
             bindSocket.Listen(5);
             Socket acceptSocket = null;
             int senderNumber = 0;
             int receiveNumber = 0;
             Socket sendSocket = null;

             Thread send = new Thread(() =>
                                       {

                                           sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                           sendSocket.SendBufferSize = 1024 * 1024;
                                           sendSocket.Connect(endpoint);

                                           for (int i = 0; i < 1000000; i++)
                                           {
                                               try
                                               {
                                                   SocketError error;
                                                   var sentBytes = sendSocket.Send(BitConverter.GetBytes(i), 0,4,SocketFlags.None, out error);
                                                   if(sentBytes != 4 || error!= SocketError.Success)
                                                       throw new Exception();
                                               }
                                               catch (Exception e)
                                               {
                                                //  Console.WriteLine(e);
                                                   senderNumber = i;
                                                  Console.WriteLine(string.Format("send i = {0}", i));
                                                   return;
                                               }
                                           }


                                       });




             Thread receive = new Thread(() =>
                                             {
                                                 acceptSocket = bindSocket.Accept();

                                                 for (int i = 0; i < 1000000; i++)
                                                 {
                                                     try
                                                     {
                                                         var buff = new byte[4];
                                                         var received = acceptSocket.Receive(buff);
                                                         if(received == 0)
                                                         {
                                                             receiveNumber = i;
                                                             Console.WriteLine(string.Format("graceful receive i = {0}", i));
                                                         }
                                                         if (i != BitConverter.ToInt32(buff, 0))
                                                             Assert.Fail();
                                                     }
                                                     catch (Exception e)
                                                     {
                                                         Console.WriteLine(e);
                                                         receiveNumber = i;
                                                         Console.WriteLine(string.Format("receive i = {0}", i));
                                                         return;
                                                     }
                                                 }
                                                 
                                             });

             send.Start();
             receive.Start();
             Thread.Sleep(500);
             acceptSocket.Shutdown(SocketShutdown.Send);
             bindSocket.Dispose();
             send.Join();

             receive.Join();
             Assert.AreEqual(senderNumber , receiveNumber);

         }
    }
}