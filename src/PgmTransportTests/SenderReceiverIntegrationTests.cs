using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PgmTransport;

namespace PgmTransportTests
{
    [TestFixture]
    public class SenderReceiverIntegrationTests
    {
        private int _messNumber;
        private int _messSentNumber;
        private byte[] _sentBuffer;


        [Test]
        public void tcp()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4net.config"));
            var waitForMessage1 = new ManualResetEvent(false);
            var waitForMessage2 = new ManualResetEvent(false);
            var waitForMessage3 = new ManualResetEvent(false);
            var sender = new TcpSender();
            var receiver = new TcpReceiver();



            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
            Thread.Sleep(1000);

            receiver.ListenToEndpoint(ipEndPoint);
            receiver.OnMessageReceived += (endpoint, stream) => OnMessageReceived(endpoint, stream, waitForMessage1, waitForMessage2, waitForMessage3, ipEndPoint, null, null);


            Thread senderThread = new Thread(() =>
            {
                _sentBuffer = Encoding.ASCII.GetBytes(String.Join("",Enumerable.Range(0,1000).Select(x => x.ToString())));
                sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
                Console.WriteLine("after first sends");

                waitForMessage1.WaitOne();
             //   waitForMessage2.WaitOne();
           //     waitForMessage3.WaitOne();
                waitForMessage1.Reset();
           //     waitForMessage2.Reset();
             //   waitForMessage3.Reset();
                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("Entering loop");
                    var watch = new Stopwatch();
                    watch.Start();
                    for (int i = 0; i < 10000; i++)
                    {
                        _messSentNumber++;
                        // //               if (_messSentNumber % 1000 == 0)
                        //                   Console.WriteLine("sending mess num" + _messSentNumber);
                        sender.Send(ipEndPoint, _sentBuffer);

                    }
                    sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));

                    waitForMessage1.WaitOne();
                 //   waitForMessage2.WaitOne();
                //    waitForMessage3.WaitOne();
                    waitForMessage1.Reset();
             //       waitForMessage2.Reset();
              //      waitForMessage3.Reset();

                    watch.Stop();

                    Console.WriteLine(string.Format("elapsed for async sends and receiving = : {0} ms", watch.ElapsedMilliseconds));
                }
            });

            senderThread.Start();
            senderThread.Join();
            sender.Dispose();
            receiver.Dispose();

            Thread.Sleep(1000);
        }

        [Test, Repeat(3)]
        public void should_send_and_receive_a_message()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4net.config"));
            var waitForMessage1 = new ManualResetEvent(false);
            var waitForMessage2 = new ManualResetEvent(false);
            var waitForMessage3 = new ManualResetEvent(false);
            var sender = new PgmSender();
            var receiver = new PgmReceiver();
            


            var ipEndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.1"), 2000);
            var ipEndPoint2 = new IPEndPoint(IPAddress.Parse("224.0.0.2"), 2001);
            var ipEndPoint3 = new IPEndPoint(IPAddress.Parse("224.0.0.3"), 2002);
            receiver.ListenToEndpoint(ipEndPoint);
            receiver.ListenToEndpoint(ipEndPoint2);
            receiver.ListenToEndpoint(ipEndPoint3);
            receiver.OnMessageReceived += (endpoint, stream) => OnMessageReceived(endpoint, stream, waitForMessage1, waitForMessage2,waitForMessage3, ipEndPoint, ipEndPoint2, ipEndPoint3);


            Thread senderThread =new Thread(() =>
                                                {
                                                    _sentBuffer = Encoding.ASCII.GetBytes(String.Join("", Enumerable.Range(0, 1000).Select(x => x.ToString())));
                                                    sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
                                                    sender.Send(ipEndPoint2, Encoding.ASCII.GetBytes("stop"));
                                                    sender.Send(ipEndPoint3, Encoding.ASCII.GetBytes("stop"));
                                                    Console.WriteLine("after first sends");

                                                    waitForMessage1.WaitOne();
                                                    waitForMessage2.WaitOne();
                                                    waitForMessage3.WaitOne();
                                                    waitForMessage1.Reset();
                                                    waitForMessage2.Reset();
                                                    waitForMessage3.Reset();
                                                    for (int j = 0; j < 10; j++)
                                                    {
                                                        Console.WriteLine("Entering loop");
                                                        var watch = new Stopwatch();
                                                        watch.Start();
                                                        for (int i = 0; i < 10000; i++)
                                                        {
                                                            _messSentNumber++;
                                            // //               if (_messSentNumber % 1000 == 0)
                                             //                   Console.WriteLine("sending mess num" + _messSentNumber);
                                                            sender.SendAsync(ipEndPoint, _sentBuffer);
                                                            sender.SendAsync(ipEndPoint2, _sentBuffer);
                                                            sender.SendAsync(ipEndPoint3, _sentBuffer);
                                                        //       sender.SendAsync(ipEndPoint, buffer);
                                                         //      sender.SendAsync(ipEndPoint2, buffer);

                                                        }
                                                        sender.SendAsync(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
                                                        sender.Send(ipEndPoint2, Encoding.ASCII.GetBytes("stop"));
                                                        sender.Send(ipEndPoint3, Encoding.ASCII.GetBytes("stop"));

                                                        waitForMessage1.WaitOne();
                                                        waitForMessage2.WaitOne();
                                                        waitForMessage3.WaitOne();
                                                        waitForMessage1.Reset();
                                                        waitForMessage2.Reset();
                                                        waitForMessage3.Reset();

                                                        watch.Stop();

                                                        Console.WriteLine(string.Format("elapsed for async sends and receiving = : {0} ms", watch.ElapsedMilliseconds));
                                                    }
                                                });

            senderThread.Start();
            senderThread.Join();
            sender.Dispose();
            receiver.Dispose();

            Thread.Sleep(1000);


        }

        private void OnMessageReceived(IPEndPoint endpoint, Stream stream, EventWaitHandle waitForMessage1, EventWaitHandle waitForMessage2, EventWaitHandle waitForMessage3, IPEndPoint ipEndPoint1, IPEndPoint ipEndPoint2, IPEndPoint ipEndPoint3)
        {

            _messNumber ++;
            //if(_messNumber %1000 == 0)
            //    Console.WriteLine("processing mess num" +_messNumber);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);

   //         var message = Encoding.ASCII.GetString(buffer);
            //     Console.WriteLine(message);
       //     if(stream.Length !=4)
      //      Assert.AreEqual(message.Length, _sentBuffer.Length);
            

            if (stream.Length == 4 && Equals(endpoint, ipEndPoint1))
            {
                Console.WriteLine("stop on endpoing" + ipEndPoint1);
                waitForMessage1.Set();

            }
            if (stream.Length == 4 && Equals(endpoint, ipEndPoint2))
            {
                Console.WriteLine("stop on endpoing" + ipEndPoint2);
                waitForMessage2.Set();

            }
            if (stream.Length == 4 && Equals(endpoint, ipEndPoint3))
            {
                Console.WriteLine("stop on endpoing" + ipEndPoint3);
                waitForMessage3.Set();

            }
            stream.Dispose();
        }

        private void OnMessageReceived(IPEndPoint endpoint, Stream stream, AutoResetEvent waitForMessage, AutoResetEvent waitForMessage2)
        {
           
        }


    }
}
