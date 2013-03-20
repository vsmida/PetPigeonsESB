using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bus.Transport.Network;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using Bus;
using Bus.Dispatch;
using Bus.MessageInterfaces;
using Bus.Startup;
using Bus.Transport;
using StructureMap;
using Tests.Transport;
using log4net;

namespace Tests.Integration
{
 

    [TestFixture]
    public class SimpleMessageExchange
    {
        private AutoResetEvent _waitForCommandToBeHandled;
        private int _persitentMessageNumber;
        private ILog _logger = LogManager.GetLogger(typeof (SimpleMessageExchange));

        [Test, Timeout(800000), Repeat(2)]
        public void should_be_able_to_exchange_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new FakeAssemblyScanner());
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2

            bus1.Initialize();
            bus2.Initialize();

            _waitForCommandToBeHandled = new AutoResetEvent(false);
            FakeCommandHandler.OnCommandReceived += OnCommandReceived;

            bus1.Send(new FakeNumberCommand(5));

            _waitForCommandToBeHandled.WaitOne();

            bus1.Dispose();
            bus2.Dispose();

        }

        [Test, Timeout(100000)]
        public void should_not_try_sending_to_disconnected_endpoints()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var containerBus1 = new Container();
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new FakeAssemblyScanner(), container:containerBus1);
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2

            var heartbeatConfig = containerBus1.GetInstance<IHeartbeatingConfiguration>();
            var sender = containerBus1.GetInstance<ZmqPushWireSendingTransport>();
            int numberOfTimesDisconnectedRaisedByTransport = 0;
            sender.EndpointDisconnected += s => numberOfTimesDisconnectedRaisedByTransport++;
            bus1.Initialize();
            bus2.Initialize();


            bus1.Send(new FakeNumberCommand(5)).WaitForCompletion();
            bus2.Dispose();
            Thread.Sleep(heartbeatConfig.HeartbeatInterval.Add(TimeSpan.FromSeconds(1))); //should be disconnected;
            for (int i = 0; i < 20000; i++)
            {
                bus1.Send(new FakeNumberCommand(1));
                
            }
            
            Assert.AreEqual(numberOfTimesDisconnectedRaisedByTransport, 0);  //would have been raised due to message pressure getting too high, test should be refactored
            bus1.Send(new FakeNumberCommand(5)).WaitForCompletion();
            
            bus1.Dispose();
        }

        public class FakeAssemblyScanner : AssemblyScanner
        {
             
            public override List<Type> GetHandledCommands()
            {
                var result = base.GetHandledCommands();
                return result.Where(x => x != typeof(FakeNumberCommand) && x != typeof(FakePersistingCommand) && x!=typeof(TestData.FakeCommand)).ToList();
            }
        }

        [Test, Timeout(200000000), Repeat(10)]
        public void should_be_able_persist_message()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new FakeAssemblyScanner());
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort1, busName1,
                                              new FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName, new List<string> { busName2 }));

            bus1.Initialize();
            brokerForBus2.Initialize();
            bus2.Initialize();


            _waitForCommandToBeHandled = new AutoResetEvent(false);
            _persitentMessageNumber = 0;
            FakePersistingCommandHandler.OnCommandReceived -= OnPersistingCommandReceived;
            FakePersistingCommandHandler.OnCommandReceived += OnPersistingCommandReceived;

            bus1.Send(new FakePersistingCommand(1)); //check normal send when everybody up
            _waitForCommandToBeHandled.WaitOne();

            Console.WriteLine("Disposing bus2");
            bus2.Dispose(); //bus 2 i dead
            Console.WriteLine("bus2 disposed");

            bus1.Send(new FakePersistingCommand(2)); //message sent while bus2 out
            var randomPort3 = NetworkUtils.GetRandomUnusedPort();
            bus2 = FakeBusFactory.CreateFakeBus(randomPort3, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            Console.WriteLine("initializing bus2 again");
            bus2.Initialize(); //alive again
            
            bus1.Send(new FakePersistingCommand(3)); // send it as soon as possible so without proper ordering it should be processed before message 2

            _waitForCommandToBeHandled.WaitOne();
            _waitForCommandToBeHandled.WaitOne();

            if (_waitForCommandToBeHandled.WaitOne(1000))
                Assert.Fail();// if there is a fourth unwelcome message;

            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();
             Console.WriteLine("end of test");

        }

        private void OnPersistingCommandReceived(int number)
        {
            //Console.WriteLine(string.Format("processing now command no {0}", s));
            _logger.InfoFormat("Processing command no {0}", number);
            Assert.AreEqual(_persitentMessageNumber + 1, number); //throw if command is not in sequence
            _persitentMessageNumber++;
            _waitForCommandToBeHandled.Set();
        }


      



        private void OnCommandReceived(int number)
        {
            Assert.AreEqual(5, number);
            _waitForCommandToBeHandled.Set();
        }
    }
}