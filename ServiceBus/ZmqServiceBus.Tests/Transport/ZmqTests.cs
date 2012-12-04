using System;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ZeroMQ;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture, Ignore]
    public class ZmqTests
    {
        private ZmqSocket _permanentSubSocket;

        [Test]
        public void dealer_can_connect_to_two_endpoints()
        {
            var context = ZmqContext.Create();
            var router1 = context.CreateSocket(SocketType.ROUTER);
            router1.Linger = TimeSpan.Zero;
            var router2 = context.CreateSocket(SocketType.ROUTER);
            router2.Linger = TimeSpan.Zero;

            var dealer = context.CreateSocket(SocketType.DEALER);

            router1.Bind("inproc://*:74");
            router2.Bind("inproc://*:75");

            dealer.Connect("inproc://*:74");
            dealer.Connect("inproc://*:75");

            dealer.Send("Test", Encoding.ASCII);
            dealer.Send("Test", Encoding.ASCII);

            router1.Receive(Encoding.ASCII);
            Assert.AreEqual("Test",router1.Receive(Encoding.ASCII));
            router2.Receive(Encoding.ASCII);
            Assert.AreEqual("Test", router2.Receive(Encoding.ASCII));

            router1.Dispose();
            router2.Dispose();
            dealer.Dispose();
            context.Dispose();

        }

        [Test]
        public void subscriber_can_connect_to_same_endpoint_twice()
        {
            var context = ZmqContext.Create();
            var sub = context.CreateSocket(SocketType.SUB);
            var pub = context.CreateSocket(SocketType.PUB);

            pub.Bind("inproc://toto");
            sub.Connect("inproc://toto");
            sub.Connect("inproc://toto");

            pub.Dispose();
            sub.Dispose();
            context.Dispose();
        }


        [Test]
        public void subscriber_can_connect_when_thread_dead()
        {
            var context = ZmqContext.Create();
            var context2 = ZmqContext.Create();
            Poller poller = new Poller();
            var pub = context.CreateSocket(SocketType.PUB);
            var pub2 = context2.CreateSocket(SocketType.PUB);
            pub.Bind("tcp://*:111");
            pub2.Bind("tcp://*:222");
            var creationThread = new Thread(() =>
                                                {
                                                    _permanentSubSocket = context.CreateSocket(SocketType.SUB);
                                                    _permanentSubSocket.Connect("tcp://localhost:111");
                                                    
                                                    //while(true)
                                                    //{
                                                    //    _permanentSubSocket.Receive(Encoding.ASCII, TimeSpan.FromMilliseconds(1));
                                                    //}
                                                }){IsBackground = true};
            creationThread.Start();
            Thread.Sleep(200);
       //     creationThread.Join();
            pub.Send("toto", Encoding.ASCII);
            Thread.Sleep(200);
            _permanentSubSocket.Connect("tcp://localhost:111");
            _permanentSubSocket.Connect("tcp://localhost:222");
            pub.Send("toto", Encoding.ASCII);


            pub.Dispose();
            pub2.Dispose();
            _permanentSubSocket.Dispose();
            context.Dispose();
        }

    }
}