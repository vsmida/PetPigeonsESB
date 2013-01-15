using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bus;
using NUnit.Framework;
using Shared;

namespace Tests.Integration
{
    public class Performance
    {

        [Test]
        public void should_send_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner());
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort2, busName2,
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName, new List<string> { busName2 }));

            bus1.Initialize();
            bus2.Initialize();
            brokerForBus2.Initialize();


            //small micro-benchmark

            for (int j = 0; j < 10; j++)
            {
                SendMessages(bus1);
            }



            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();

        }

        private static void SendMessages(IBus bus1)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            IBlockableUntilCompletion resetEvent = null;
            for (int i = 0; i < 10000; i++)
            {
                resetEvent = bus1.Send(new FakePersistingCommand(i + 4));
            }

            resetEvent.WaitForCompletion();

            watch.Stop();
            Console.WriteLine(" 10000 resend took " + watch.ElapsedMilliseconds + " ms");
        }
    }
}