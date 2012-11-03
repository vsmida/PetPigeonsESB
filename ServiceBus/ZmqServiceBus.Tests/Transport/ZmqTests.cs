using System;
using System.Text;
using NUnit.Framework;
using ZeroMQ;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture, Ignore]
    public class ZmqTests
    {
         
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

    }
}