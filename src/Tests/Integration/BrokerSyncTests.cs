using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bus;
using Bus.Handlers;
using Bus.Transport.Network;
using NUnit.Framework;
using Shared;
using StructureMap;
using System.Linq;

namespace Tests.Integration
{
    [TestFixture]
    public class BrokerSyncTests
    {
        private AutoResetEvent _waitForCommandToBeHandled;
        private int _persitentMessageNumber;

        [Test, Timeout(15000), Repeat(3)]
        public void should_send_back_all_messages()
        {
            var randomPort1 = NetworkUtils.GetRandomUnusedPort();
            var randomPort2 = NetworkUtils.GetRandomUnusedPort();
            var randomPortBroker = NetworkUtils.GetRandomUnusedPort();
            var busName1 = "Service1";
            var busName2 = "Service2";
            var brokerName = "Service2Shadow";
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner());
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerContainer = new Container();
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort2, busName2,
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName, new List<string> { busName2 }), container: brokerContainer);

            bus1.Initialize();
            bus2.Initialize();
            brokerForBus2.Initialize();


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
            bus2 = FakeBusFactory.CreateFakeBus(randomPort3, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            Console.WriteLine("initializing bus2 again");
            bus2.Initialize(); //alive again

            var completionCallback = bus1.Send(new FakePersistingCommand(2002)); // send it as soon as possible so without proper ordering it should be processed before message 2

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
            var bus1 = FakeBusFactory.CreateFakeBus(randomPort1, busName1, randomPort1, busName1, assemblyScanner: new SimpleMessageExchange.FakeAssemblyScanner(), container: bus1Container);
            var bus2 = FakeBusFactory.CreateFakeBus(randomPort2, busName2, randomPort1, busName1); //bus2 knows bus1 (ie bus1 acts as directory service for bus2
            var brokerContainer = new Container();
            var brokerForBus2 = FakeBusFactory.CreateFakeBus(randomPortBroker, brokerName, randomPort2, busName2,
                                              new SimpleMessageExchange.FakeAssemblyScanner(),
                                              new DummyPeerConfig(brokerName, new List<string> { busName2 }), container: brokerContainer);

            bus1.Initialize();
            bus2.Initialize();
            brokerForBus2.Initialize();

            bool disconnectOccured = false;
            var bus1ZmqSender = bus1Container.GetInstance<ZmqPushWireSendingTransport>();
            bus1ZmqSender.EndpointDisconnected += x =>disconnectOccured = true;
                
            for (int i = 0; i < 20000; i++)
            {
                bus1.Send(new FakePersistingCommand(i)); 
            }
            var completionCallback = bus1.Send(new FakePersistingCommand(20001));
            completionCallback.WaitForCompletion();


            bus1.Dispose();
            bus2.Dispose();
            brokerForBus2.Dispose();

            var messageStore = brokerContainer.GetInstance<ISavedMessagesStore>();
            var remainingMessages = messageStore.GetFirstMessages(busName2, null).ToList();
            Assert.AreEqual(0, remainingMessages.Count);
            Assert.IsTrue(disconnectOccured);


        }

        private void OnPersistingCommandReceived(int number)
        {
            //Console.WriteLine(string.Format("processing now command no {0}", s));
            Assert.AreEqual(_persitentMessageNumber + 1, number); //throw if command is not in sequence
            _persitentMessageNumber++;
            _waitForCommandToBeHandled.Set();
        }


      
    }
}