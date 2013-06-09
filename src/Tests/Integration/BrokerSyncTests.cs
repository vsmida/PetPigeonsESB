using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bus;
using Bus.Handlers;
using Bus.Transport;
using Bus.Transport.Network;
using NUnit.Framework;
using Shared;
using StructureMap;
using System.Linq;
using log4net;

namespace Tests.Integration
{
    [TestFixture]
    public class BrokerSyncTests
    {
        private AutoResetEvent _waitForCommandToBeHandled;
        private int _persitentMessageNumber;
        private ILog _logger = LogManager.GetLogger(typeof(BrokerSyncTests));
        private bool _shouldTakeAYearProcessing;


        [Test, Timeout(150000), Repeat(3)]
        public void should_send_back_all_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1,new  PeerId(1), randomPort1, busName1,new PeerId(1), assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner());
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, new PeerId(2), randomPort1, busName1, new PeerId(1)); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerContainer = new Container();
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, new PeerId(3), randomPort1, busName1, new PeerId(1),
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName,new PeerId(1), new List<ShadowedPeerConfiguration> { new ShadowedPeerConfiguration(new PeerId(2), true) }), container: brokerContainer);

            bus1.Initialize();
            brokerForBus2.Initialize(); //todo: this is wrong, broker should be able to start at any time, should implement retry or abandon and say we are initialized after a timeout
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


            for (int i = 0; i < 2000; i++)
            {
                bus1.Send(new FakePersistingCommand(i+2)); //message sent while bus2 out
                
            }

            var randomPort3 = NetworkUtils.GetRandomUnusedPort();
            bus2 = FakeBusFactory.CreateFakeBus(randomPort3, busName2, new PeerId(2), randomPort1, busName1, new PeerId(1)); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            Console.WriteLine("initializing bus2 again");
            bus2.Initialize(); //alive again

            var completionCallback = bus1.Send(new FakePersistingCommand(2002)); 
            // send it as soon as possible so without proper ordering it should be processed before message 2
            //todo: hook something so that we are sure it arrives first, peer connection in the broker?
            completionCallback.WaitForCompletion();

            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();
            Console.WriteLine("end of test");

        }

        [Test]
        public void broker_should_not_crash_when_missing_acks_or_messages_due_to_disconnect()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1Container = new Container();
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, new PeerId(1), randomPort1, busName1, new PeerId(1), assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner(), container: bus1Container);
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, new PeerId(2), randomPort1, busName1, new PeerId(1)); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerContainer = new Container();
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, new PeerId(3), randomPort1, busName1, new PeerId(1),
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName,new PeerId(3), new List<ShadowedPeerConfiguration> { new ShadowedPeerConfiguration(new PeerId(2), true) }), container: brokerContainer);

            bus1.Initialize();
            brokerForBus2.Initialize();
            bus2.Initialize();
            _waitForCommandToBeHandled = new AutoResetEvent(false);
            bool disconnectOccured = false;
            var bus1ZmqSender = bus1Container.GetInstance<ZmqPushWireSendingTransport>();
            bus1ZmqSender.EndpointDisconnected += x =>disconnectOccured = true;
            bus1.Send(new FakePersistingCommand(1)).WaitForCompletion(); //init message

            _persitentMessageNumber = -1;
            FakePersistingCommandHandler.OnCommandReceived -= OnPersistingCommandReceived;
            FakePersistingCommandHandler.OnCommandReceived += OnPersistingCommandReceived;
            _shouldTakeAYearProcessing = true;
            for (int i = 0; i < 20000; i++)
            {
                bus1.Send(new FakePersistingCommand(i));
                Thread.Sleep(1);
            }
            var completionCallback = bus1.Send(new FakePersistingCommand(20001));
            _shouldTakeAYearProcessing = false;
            completionCallback.WaitForCompletion();


            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();

            var messageStore = brokerContainer.GetInstance<ISavedMessagesStore>();
            var remainingMessages = messageStore.GetFirstMessages(new PeerId(2), null).ToList();
            Assert.AreEqual(0, remainingMessages.Count);
            Assert.IsTrue(disconnectOccured);


        }

        private void OnPersistingCommandReceived(int number)
        {
            _logger.InfoFormat("Processing command no {0}", number);
            Assert.AreEqual(_persitentMessageNumber + 1, number); //throw if command is not in sequence
            if(_shouldTakeAYearProcessing)
            Thread.Sleep(10000);
            _persitentMessageNumber++;
            _waitForCommandToBeHandled.Set();
        }


      
    }
}