using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bus;
using NUnit.Framework;
using Shared;
using log4net;

namespace Tests.Integration
{
    [TestFixture]
    public class PerformanceTests
    {
        private ILog _logger = LogManager.GetLogger(typeof(PerformanceTests));

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
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort1, busName1,
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName, new List<string> { busName2 }));

            bus1.Initialize();
            brokerForBus2.Initialize();

            bus2.Initialize();
            FakePersistingCommandHandler.OnCommandReceived -= OnPersistingCommandReceived;
            FakePersistingCommandHandler.OnCommandReceived += OnPersistingCommandReceived;

            //small micro-benchmark

            for (int j = 0; j < 10000; j++)
            {
                SendMessages(bus1, j);
            }



            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();

        }

        private void OnPersistingCommandReceived(int obj)
        {
            _logger.Debug(string.Format("processing command no {0}", obj));
        }

        private static void SendMessages(IBus bus1, int loopNumber)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            IBlockableUntilCompletion resetEvent = null;
            for (int i = 0; i < 1000; i++)
            {
                resetEvent = bus1.Send(new FakeNumberCommand(i * (loopNumber + 1)));
            }

            resetEvent.WaitForCompletion();

            watch.Stop();
            Console.WriteLine(" 10000 resend took " + watch.ElapsedMilliseconds + " ms");
        }
    }
}