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
using Shared;
using ZeroMQ;
using ZeroMQ.Interop;

namespace PgmTransportTests
{
    [TestFixture]
    public class SenderReceiverIntegrationTests
    {
        private int _messNumber;
        private int _messSentNumber;
        private byte[] _sentBuffer;
        private int _currentNumber;
        private bool _running;

        [SetUp]
        public void setup()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Log4net.config"));




        }

        [Test, Timeout(2000000), Repeat(10)]
        public void CheckAllMessagesInOrder()
        {
            _currentNumber = 1;
            var port = NetworkUtils.GetRandomUnusedPort();
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            var transport = new SendingTransport();
            var sender = new TcpTransportPipeMultiThread(100000, HighWaterMarkBehavior.Block, ipEndPoint, transport);
            var receiver = new TcpReceiver();

            var waitHandle = new AutoResetEvent(false);
            var batchSize = 20000;
            receiver.RegisterCallback(ipEndPoint, s => OnCheckErrorMessageReceived(s, waitHandle, batchSize));


            var beforebindDataCount = 100;
            for (int i = 1; i < beforebindDataCount; i++)
            {
                sender.Send(new ArraySegment<byte>(BitConverter.GetBytes(i)));                
            }
            Thread.Sleep(800);
            receiver.ListenToEndpoint(ipEndPoint);
            SpinWait.SpinUntil(() => _currentNumber == beforebindDataCount);
            Console.WriteLine("received first messages");
            var watch = new Stopwatch();
            watch.Start();
            for (int i = beforebindDataCount; i < batchSize / 2; i++)
            {
                sender.Send(new ArraySegment<byte>(BitConverter.GetBytes(i)));
            }
            Console.WriteLine("stoppping reception on endpoint");

            SpinWait.SpinUntil(() => _currentNumber >= batchSize / 2);

            
            receiver.StopListeningTo(ipEndPoint);
            Thread.Sleep(100);
            Console.WriteLine("re-establishing reception on endpoint");


            sender.Send(new ArraySegment<byte>(BitConverter.GetBytes(batchSize / 2)));
            sender.Send(new ArraySegment<byte>(BitConverter.GetBytes(batchSize / 2 + 1)));
            Thread.Sleep(100);
            receiver.ListenToEndpoint(ipEndPoint);
            Thread.Sleep(500);


            for (int i = batchSize / 2 +2; i < batchSize; i++)
            {
              //  sender.Send(ipEndPoint, BitConverter.GetBytes(i));
                sender.Send(new ArraySegment<byte>(BitConverter.GetBytes(i)));
            }
            Console.WriteLine("sent last messages");

            waitHandle.WaitOne();
            if(_currentNumber != batchSize)
                Assert.Fail();
            watch.Stop();
            var fps = batchSize / (watch.ElapsedMilliseconds / 1000m);
            Console.WriteLine(string.Format("FPS = : {0} mess/sec, elapsed : {1} ms, messages {2}", fps.ToString("N2"), watch.ElapsedMilliseconds, batchSize));

            sender.Dispose();
            transport.Dispose();
            receiver.Dispose();

            Thread.Sleep(500);
        }

        private void OnCheckErrorMessageReceived(Stream stream, AutoResetEvent waitHandle, int batchSize)
        {
            var buff = new byte[4];
            stream.Read(buff, 0, 4);
            var number = BitConverter.ToInt32(buff, 0);
            if (number != _currentNumber)
            {
                Console.WriteLine(string.Format("current number = {0}, received number = {1}, batchSize /2 = {2}", _currentNumber, number, batchSize / 2));
                waitHandle.Set();
                return;
            }
            _currentNumber++;
            if (_currentNumber % 100000 == 0)
                Console.WriteLine(string.Format("current number =  {0}", _currentNumber));
            
            if (_currentNumber == batchSize)
                waitHandle.Set();

        }

        private void OnMessageReceived(IPEndPoint endpoint, Stream stream, AutoResetEvent waitForMessage, AutoResetEvent waitForMessage2)
        {

        }

        [Test]
        public void tcp_zmq()
        {
            var context1 = ZmqContext.Create();
            var context2 = ZmqContext.Create();
            Poller poll = new Poller();
            var waitForMessage1 = new ManualResetEvent(false);
            var ipEndPoint = "tcp://*:2000";
            var sendEndpoint = "tcp://localhost:2000";
            var sender = context1.CreateSocket(SocketType.PUSH);
            sender.SendHighWatermark = 9999999;
            var receiver = context2.CreateSocket(SocketType.PULL);
            receiver.ReceiveHighWatermark = 99999999;
            receiver.Bind(ipEndPoint);
            sender.Connect(sendEndpoint);
            receiver.ReceiveReady += (o,s) => OnZmqReceive(o,s, waitForMessage1);
            poll.AddSocket(receiver);

            Thread.Sleep(1000);
            _running = true;
            var rThread = new BackgroundThread(() =>
                                                            {
                                                                while (_running)
                                                                {
                                                                    var received = receiver.Receive();
                                                                    _messNumber++;

                                                                    if (received.Length == 4)
                                                                    {
                                                                        Console.WriteLine("stop on zmq endpoing");
                                                                        waitForMessage1.Set();
                                                                    }


                                                                }
                                                            });
            rThread.Start();

            Thread senderThread = new Thread(() =>
            {
                _sentBuffer = Encoding.ASCII.GetBytes(String.Join("", Enumerable.Range(0, 40).Select(x => x.ToString())));
                sender.Send(Encoding.ASCII.GetBytes("stop"));
                
                _messSentNumber++;
                Console.WriteLine("after first sends");

                waitForMessage1.WaitOne();
                waitForMessage1.Reset();
                Thread.Sleep(1000);

                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("Entering loop");
                    var watch = new Stopwatch();
                    watch.Start();
                    var batchSize = 1000000;
                    for (int i = 0; i < batchSize; i++)
                    {
                        _messSentNumber++;
                        sender.Send(_sentBuffer);

                    }
                    sender.Send(Encoding.ASCII.GetBytes("stop"));
                    _messSentNumber++;

                    waitForMessage1.WaitOne();
                    waitForMessage1.Reset();
                    Assert.AreEqual(_messSentNumber, _messNumber);
                    watch.Stop();
                    var fps = batchSize / (watch.ElapsedMilliseconds / 1000m);
                    Console.WriteLine(string.Format("FPS = : {0} mess/sec, elapsed : {1} ms, messages {2}", fps.ToString("N2"), watch.ElapsedMilliseconds, batchSize));
                }
            });
            senderThread.Start();
            senderThread.Join();
            _running = false;
            receiver.Dispose();
            rThread.Join();
            sender.Dispose();
            context1.Dispose();
            context2.Dispose();
            Thread.Sleep(1000);
        }



        private void OnZmqReceive(object sender, SocketEventArgs e, ManualResetEvent waitForMessage1)
        {
            var socket = e.Socket;
            var received = socket.Receive();
            _messNumber++;
            var position = 0;
            byte b;
            while (!(position >= received.Length))
            {
                b = received[position];
                position++;
            }//read stream
            if (received.Length == 4)
            {

                Console.WriteLine("stop on zmq endpoing" );
                waitForMessage1.Set();

            }
          //  stream.Dispose();
        }


        [Test]
        public void tcp()
        {
            var waitForMessage1 = new ManualResetEvent(false);
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            var transport = new SendingTransport();
            var sender = new TcpTransportPipeMultiThread(600000, HighWaterMarkBehavior.Block, ipEndPoint, transport);
            var receiver = new TcpReceiver();


            Thread.Sleep(1000);
            receiver.RegisterCallback(ipEndPoint, s => OnIpEndpointMessageReceived(s,waitForMessage1,ipEndPoint));
            receiver.ListenToEndpoint(ipEndPoint);


            Thread senderThread = new Thread(() =>
            {
                _sentBuffer = Encoding.ASCII.GetBytes(String.Join("", Enumerable.Range(0, 40).Select(x => x.ToString())));
              //  sender.Send(ipEndPoint, Encoding.ASCII.GetBytes("stop"));
                sender.Send(new ArraySegment<byte>(Encoding.ASCII.GetBytes("stop")));
                _messSentNumber++;
                Console.WriteLine("after first sends");

                waitForMessage1.WaitOne();
                waitForMessage1.Reset();
                Thread.Sleep(1000);

                for (int j = 0; j < 10; j++)
                {
                    Console.WriteLine("Entering loop");
                    var watch = new Stopwatch();
                    watch.Start();
                    var batchSize = 1000000;
                    for (int i = 0; i < batchSize; i++)
                    {
                        _messSentNumber++;
                        sender.Send(new ArraySegment<byte>(_sentBuffer));

                    }
                    sender.Send(new ArraySegment<byte>(Encoding.ASCII.GetBytes("stop")));
                    _messSentNumber++;
                  //  sender.Send(ipEndPoint, Encoding.ASCII.GetBytes("stop"));

                    waitForMessage1.WaitOne();
                    waitForMessage1.Reset();
                    watch.Stop();

                    Assert.AreEqual(_messSentNumber, _messNumber);
                    var fps = batchSize / (watch.ElapsedMilliseconds / 1000m);
                    Console.WriteLine(string.Format("FPS = : {0} mess/sec, elapsed : {1} ms, messages {2}", fps.ToString("N2"), watch.ElapsedMilliseconds, batchSize));
                }
            });

            senderThread.Start();
            senderThread.Join();
            sender.Dispose();
            receiver.Dispose();

            Thread.Sleep(1000);
        }

       private void OnIpEndpointMessageReceived(Stream stream, EventWaitHandle waitHandle, IPEndPoint endpoint)
        {
            _messNumber++;
           var position = 0;
           var length = stream.Length;
           while (!(position >= length))
           {
                stream.ReadByte();
               position++;
           }//read stream
            if (length == 4)
            {

                Console.WriteLine("stop on endpoing" + endpoint);
                waitHandle.Set();

            }
            stream.Dispose();

        }

  




    }
}
