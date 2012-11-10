using System.Collections.Concurrent;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Transport;
using Serializer = Shared.Serializer;
using Shared.TestTools;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class TransportTests
    {
        private ZmqServiceBus.Transport.EndpointManager _endpointManager;
        private Mock<IZmqSocketManager> _socketManagerMock;
        private FakeTransportConfiguration _configuration;
        [ProtoContract]
        private class FakeCommand : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public readonly int Test;

            public FakeCommand(int test)
            {
                Test = test;
            }

            private FakeCommand()
            {

            }
        }

        [ProtoContract]
        private class FakeCommand2 : ICommand
        {
            [ProtoMember(1, IsRequired = true)]
            public readonly int Test;

            public FakeCommand2(int test)
            {
                Test = test;
            }

            private FakeCommand2()
            {

            }
        }
        [ProtoContract]
        private class FakeEvent : IEvent
        {
            [ProtoMember(1, IsRequired = true)]
            public readonly int Test;

            public FakeEvent(int test)
            {
                Test = test;
            }

            private FakeEvent()
            {

            }
        }


        [SetUp]
        public void setup()
        {
            _configuration = new FakeTransportConfiguration();
            _socketManagerMock = new Mock<IZmqSocketManager>();
            _endpointManager = new ZmqServiceBus.Transport.EndpointManager(_configuration, _socketManagerMock.Object);
        }

        [Test]
        public void should_create_a_socket_to_handle_incoming_commands_on_init()
        {
            _endpointManager.Initialize();

            var endpoint = _configuration.CommandsProtocol + "://*:" + _configuration.CommandsPort;
            _socketManagerMock.Verify(x => x.CreateResponseSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.PeerName));
        }

        [Test]
        public void should_create_a_socket_to_handle_outgoing_events_on_init()
        {
            _endpointManager.Initialize();

            var endpoint = _configuration.EventsProtocol + "://*:" + _configuration.EventsPort;
            _socketManagerMock.Verify(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.PeerName));
        }

        [Test]
        public void should_create_a_sub_socket_on_init()
        {
            _endpointManager.Initialize();

            var endpoint = _configuration.EventsProtocol + "://*:" + _configuration.EventsPort;
            _socketManagerMock.Verify(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>()));
        }


        [Test]
        public void should_register_command_handling_endpoint_and_create_socket_lazily()
        {
            BlockingCollection<ITransportMessage> sendQueue = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>(
                    (x, y, z, t) => sendQueue = x);

            _endpointManager.Initialize();
            var endpoint = "endpoint";
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            var fakeCommand = new FakeCommand(2);
            var transportMessage = TestData.GenerateDummyMessage(fakeCommand);
            _endpointManager.SendMessage(transportMessage);

            _socketManagerMock.VerifyAll();
            var sentTransportMessage = sendQueue.Take();
            Assert.AreEqual(transportMessage.PeerName, sentTransportMessage.PeerName);
            Assert.AreEqual(typeof(FakeCommand).FullName, sentTransportMessage.MessageType);
            Assert.AreEqual(2, Serializer.Deserialize<FakeCommand>(sentTransportMessage.Data).Test);
        }

        [Test]
        public void should_send_commands_to_all_known_endpoints()
        {
            var endpoint = "endpoint";
            var endpoint2 = "endpoint2";
            BlockingCollection<ITransportMessage> sendQueue1 = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, It.IsAny<string>()))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>(
                    (x, y, z, t) => sendQueue1 = x);
            BlockingCollection<ITransportMessage> sendQueue2 = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint2, It.IsAny<string>()))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>(
                    (x, y, z, t) => sendQueue2 = x);

            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint2));
            var fakeCommand = new FakeCommand(2);
            var transportMessage = TestData.GenerateDummyMessage(fakeCommand);

            _endpointManager.SendMessage(transportMessage);

            _socketManagerMock.VerifyAll();
            var sentTransportMessage1 = sendQueue1.Take();
            Assert.AreEqual(transportMessage.PeerName, sentTransportMessage1.PeerName);
            Assert.AreEqual(typeof(FakeCommand).FullName, sentTransportMessage1.MessageType);
            Assert.AreEqual(2, Serializer.Deserialize<FakeCommand>(sentTransportMessage1.Data).Test);

            var sentTransportMessage2 = sendQueue2.Take();
            Assert.AreEqual(transportMessage.PeerName, sentTransportMessage2.PeerName);
            Assert.AreEqual(typeof(FakeCommand).FullName, sentTransportMessage2.MessageType);
            Assert.AreEqual(2, Serializer.Deserialize<FakeCommand>(sentTransportMessage2.Data).Test);

        }


        [Test]
        public void should_route_message_to_specific_peer()
        {
            var endpoint = "endpoint";
            var endpoint2 = "endpoint";
            BlockingCollection<ITransportMessage> sendQueueForFakeCommand = null;
            _endpointManager.Initialize();
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.PeerName))
                  .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>((x, y, z, t) => sendQueueForFakeCommand = x);

            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint, "P1"));
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint2, "P2"));

            var transportMessage = TestData.GenerateDummyMessage<FakeCommand>("P1");
            _endpointManager.RouteMessage(transportMessage);

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            var sentMessage = sendQueueForFakeCommand.Take();
            Assert.AreEqual(transportMessage.Data,sentMessage.Data);
            Assert.AreEqual(transportMessage.MessageIdentity,sentMessage.MessageIdentity);
            Assert.AreEqual(transportMessage.MessageType,sentMessage.MessageType);
            Assert.AreEqual(transportMessage.PeerName,sentMessage.PeerName);
        }


        [Test]
        public void should_send_message_on_proper_socket()
        {
            var endpoint = "endpoint";
            var endpoint2 = "endpoint2";
            BlockingCollection<ITransportMessage> sendQueueForFakeCommand = null;
            BlockingCollection<ITransportMessage> sendQueueForFakeCommand2 = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.PeerName))
                              .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>((x, y, z, t) => sendQueueForFakeCommand = x);
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint2, _configuration.PeerName))
                  .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>((x, y, z, t) => sendQueueForFakeCommand2 = x);
            _endpointManager.Initialize();
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand2>(endpoint2));

            var command1 = new FakeCommand(2);
            var transportMessage = TestData.GenerateDummyMessage(command1);
            _endpointManager.SendMessage(transportMessage);
            var command2 = new FakeCommand2(3);
            var transportMessage2 = TestData.GenerateDummyMessage(command2);
            _endpointManager.SendMessage(transportMessage2);

            var fakeCommand = sendQueueForFakeCommand.Take();
            var fakeCommand2 = sendQueueForFakeCommand2.Take();
            Assert.AreEqual(typeof(FakeCommand).FullName, fakeCommand.MessageType);
            Assert.AreEqual(typeof(FakeCommand2).FullName, fakeCommand2.MessageType);
        }

        [Test]
        public void should_create_request_socket_for_endpoint_only_once_for_same_endpoint()
        {
            _endpointManager.Initialize();
            var endpoint = "endpoint";

            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand2>(endpoint));
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));

            _endpointManager.SendMessage(TestData.GenerateDummyMessage<FakeCommand>());
            _endpointManager.SendMessage(TestData.GenerateDummyMessage<FakeCommand2>());

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                                                 It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }


        [Test]
        public void should_publish_events()
        {
            BlockingCollection<ITransportMessage> messagesToSend = null;
            _socketManagerMock.Setup(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()))
                                           .Callback<BlockingCollection<ITransportMessage>, string, string>((x, y,t) => messagesToSend = x);
            _endpointManager.Initialize();
            var fakeEvent = new FakeEvent(2);
            var transportMessage = TestData.GenerateDummyMessage(fakeEvent);
           
            _endpointManager.PublishMessage(transportMessage);

            var sentMessage = messagesToSend.Take();
            Assert.AreEqual(typeof(FakeEvent).FullName, sentMessage.MessageType);
            Assert.AreEqual(transportMessage.PeerName, sentMessage.PeerName);
            Assert.AreEqual(2, Serializer.Deserialize<FakeEvent>(sentMessage.Data).Test);
        }

        [Test]
        public void should_register_publisher()
        {
            _endpointManager.Initialize();
            string endpoint = "endpoint";

            _endpointManager.RegisterPeer(TestData.CreatePeerThatPublishes<FakeEvent>(endpoint));
            _endpointManager.ListenTo<FakeEvent>();

            _socketManagerMock.Verify(x => x.SubscribeTo(endpoint, typeof(FakeEvent).FullName));
        }

        [Test]
        public void should_register_publisher_automatically_when_new_one_connects()
        {
            _endpointManager.Initialize();
            string endpoint = "endpoint";

            _endpointManager.ListenTo<FakeEvent>();
            _endpointManager.RegisterPeer(TestData.CreatePeerThatPublishes<FakeEvent>(endpoint));

            _socketManagerMock.Verify(x => x.SubscribeTo(endpoint, typeof(FakeEvent).FullName));
        }



        [Test, Timeout(1000)]
        public void should_raise_message_received_from_event()
        {
            AutoResetEvent waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.Setup(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>()))
                                         .Callback<BlockingCollection<ITransportMessage>>((x) => messagesReceived = x);
            var transportMessage = TestData.GenerateDummyMessage(new FakeEvent(2));
            _endpointManager.Initialize();
            _endpointManager.OnMessageReceived += (message) =>
                                                {
                                                    Assert.AreEqual(transportMessage.Data, message.Data);
                                                    Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                                                    Assert.AreEqual(transportMessage.PeerName, message.PeerName);
                                                    Assert.AreEqual(transportMessage.MessageIdentity, message.MessageIdentity);
                                                    waitForEvent.Set();
                                                };
            string endpoint = "endpoint";
            _endpointManager.RegisterPeer(TestData.CreatePeerThatPublishes<FakeEvent>(endpoint));
            messagesReceived.Add(transportMessage);

            waitForEvent.WaitOne();

        }

        [Test]
        public void should_raise_message_received_from_receivedAck()
        {
            var waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.CaptureVariable(() => messagesReceived, (s, x) => s.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), x, It.IsAny<string>(), It.IsAny<string>()));
            var transportMessage = TestData.GenerateDummyMessage(new FakeEvent(2));
            _endpointManager.Initialize();
            _endpointManager.OnMessageReceived += (message) =>
            {
                Assert.AreEqual(transportMessage.Data, message.Data);
                Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                Assert.AreEqual(transportMessage.PeerName, message.PeerName);
                Assert.AreEqual(transportMessage.MessageIdentity, message.MessageIdentity);
                waitForEvent.Set();
            };
            string endpoint = "endpoint";
            _endpointManager.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _endpointManager.SendMessage(TestData.GenerateDummyMessage<FakeCommand>());
            messagesReceived.Add(transportMessage);

            waitForEvent.WaitOne();

        }



        [Test]
        public void should_dispose_socket_manager()
        {
            _endpointManager.Initialize();

            _endpointManager.Dispose();

            _socketManagerMock.Verify(x => x.Stop());
        }

        [TearDown]
        public void teardown()
        {
            _endpointManager.Dispose();
        }
    }
}
