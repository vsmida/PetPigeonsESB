using System;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ZeroMQ;

namespace Tests.Transport
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
            Assert.AreEqual("Test", router1.Receive(Encoding.ASCII));
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
        public void can_detect_disconnect()
        {
            var context = ZmqContext.Create();

      

            var monitor = context.CreateMonitor();
            var adress = "inproc://toto";
            AutoResetEvent wait = new AutoResetEvent(false);
            monitor.Disconnected += (s, e) =>
                                        {
                                            Assert.AreEqual(adress, e.Address);
                                            wait.Set();
                                        };
            var sub = context.CreateSocket(SocketType.SUB);
            var pub = context.CreateSocket(SocketType.PUB);

            pub.Bind(adress);
            sub.Connect(adress);
            sub.Connect(adress);

            pub.Dispose();
            sub.Dispose();
            context.Dispose();
        }


        [Test]
        public void can_use_multiple_transport()
        {
            var context = ZmqContext.Create();
            var sub = context.CreateSocket(SocketType.SUB);
            var pub = context.CreateSocket(SocketType.PUB);
            var pub2 = context.CreateSocket(SocketType.PUB);

            pub.Bind("inproc://toto");
            pub2.Bind("tcp://*:222");
            sub.Connect("inproc://toto");
            sub.Connect("tcp://localhost:222");
         //   sub.Subscribe(Encoding.ASCII.GetBytes("pub"));
            var second = new byte[0];
            var prefix = Encoding.ASCII.GetBytes("pub2").Concat(Encoding.ASCII.GetBytes("TOTO")).ToArray();
            sub.Subscribe(prefix);
            

       //     pub.Send("pub", Encoding.ASCII);
      //      Console.WriteLine(sub.Receive(Encoding.ASCII));
            pub2.Send("pub2", Encoding.ASCII);
            pub2.SendMore("pub2", Encoding.ASCII);
            pub2.Send("TOTO", Encoding.ASCII);

            Console.WriteLine(sub.Receive(Encoding.ASCII));
            Console.WriteLine(sub.Receive(Encoding.ASCII));
            
            sub.Dispose();
            pub.Dispose();
            pub2.Dispose();
            context.Dispose();
        }


        [Test]
        public void routerTests()
        {
            var context = ZmqContext.Create();
            var sockd1 = context.CreateSocket(SocketType.ROUTER);
            var sockd2 = context.CreateSocket(SocketType.ROUTER);
            var identity = "sockd111";
            sockd1.Identity = Encoding.ASCII.GetBytes(identity);
            sockd1.Bind("tcp://*:*");
            var endpoint = sockd1.LastEndpoint;
            var endpointConnectFirst = "tcp://localhost:" + endpoint.Split(':').Last();
            sockd2.Connect(endpointConnectFirst);

            sockd2.SendMore(identity, Encoding.ASCII);
            sockd2.Send("test1Allconnected", Encoding.ASCII);
            Console.WriteLine(sockd1.Receive(Encoding.ASCII));
            sockd1.Dispose();
            sockd2.SendMore(identity, Encoding.ASCII);
            sockd2.Send("test2NoOneconnected", Encoding.ASCII);
            sockd1 = context.CreateSocket(SocketType.ROUTER);
            sockd1.Identity = Encoding.ASCII.GetBytes(identity);
            sockd1.Bind("tcp://*:*");
            sockd2.Connect("tcp://localhost:" + sockd1.LastEndpoint.Split(':').Last());
            sockd2.Disconnect(endpointConnectFirst);
            sockd2.SendMore(identity, Encoding.ASCII);
            sockd2.Send("test3ReConnected", Encoding.ASCII);
            Console.WriteLine(sockd1.Receive(Encoding.ASCII));

            sockd1.Dispose();
            sockd2.Dispose();
          //  Thread.Sleep(10000);
         context.Dispose();   
        }

    }
}