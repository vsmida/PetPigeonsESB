using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Transport;
using Serializer = Shared.Serializer;
using Shared.TestTools;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class TransportTests
    {
        private ZmqServiceBus.Transport.Transport _transport;
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
            _transport = new ZmqServiceBus.Transport.Transport(_configuration, _socketManagerMock.Object);
        }

        [Test]
        public void should_create_a_socket_to_handle_incoming_commands_on_init()
        {
            _transport.Initialize();

            var endpoint = _configuration.CommandsProtocol + "://*:" + _configuration.CommandsPort;
            _socketManagerMock.Verify(x => x.CreateResponseSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint));
        }

        [Test]
        public void should_create_a_socket_to_handle_outgoing_events_on_init()
        {
            _transport.Initialize();

            var endpoint = _configuration.EventsProtocol + "://*:" + _configuration.EventsPort;
            _socketManagerMock.Verify(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint));
        }

        [Test]
        public void should_create_a_sub_socket_on_init()
        {
            _transport.Initialize();

            var endpoint = _configuration.EventsProtocol + "://*:" + _configuration.EventsPort;
            _socketManagerMock.Verify(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>()));
        }


        [Test]
        public void should_register_command_handling_endpoint_and_create_socket_lazily()
        {
            BlockingCollection<ITransportMessage> sendQueue = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string>(
                    (x, y, z) => sendQueue = x);

            _transport.Initialize();
            var endpoint = "endpoint";
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()), Times.Never());

            var fakeCommand = new FakeCommand(2);
            var transportMessage = GetTransportMessage(fakeCommand);
            _transport.SendMessage(transportMessage);

            _socketManagerMock.VerifyAll();
            var sentTransportMessage = sendQueue.Take();
            Assert.AreEqual(transportMessage.SendingSocketId, sentTransportMessage.SendingSocketId);
            Assert.AreEqual(typeof(FakeCommand).FullName, sentTransportMessage.MessageType);
            Assert.AreEqual(2, Serializer.Deserialize<FakeCommand>(sentTransportMessage.Data).Test);
        }

        private TransportMessage GetTransportMessage(IMessage message)
        {
            var transportMessage = new TransportMessage(Guid.NewGuid(), Encoding.ASCII.GetBytes("Toto"), message.GetType().FullName,
                                                        Serializer.Serialize(message));
            return transportMessage;
        }

        private TransportMessage GetTransportMessage(IMessage message, byte[] transportMessageIdentity)
        {
            var transportMessage = new TransportMessage(Guid.NewGuid(), transportMessageIdentity, message.GetType().FullName,
                                                        Serializer.Serialize(message));
            return transportMessage;
        }

        [Test]
        public void should_send_domain_acknowledgement()
        {
            BlockingCollection<ITransportMessage> ackQueue = null;
            _socketManagerMock.Setup(x => x.CreateResponseSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string>(
                    (x, y, z) => ackQueue = y);
            _transport.Initialize();

            var messageId = Guid.NewGuid();
            var recipientIdentity = Encoding.ASCII.GetBytes("toto");
            _transport.AckMessage(recipientIdentity, messageId, true);

            Assert.AreEqual(1, ackQueue.Count);
            var transportMessage = ackQueue.Take();
            Assert.AreNotEqual(Guid.Empty, transportMessage.MessageIdentity);
            Assert.AreEqual(recipientIdentity, transportMessage.SendingSocketId);
            Assert.AreEqual(typeof(AcknowledgementMessage).FullName, transportMessage.MessageType);
            Assert.AreEqual(Serializer.Serialize(new AcknowledgementMessage(messageId, true)), transportMessage.Data);
        }


        [Test]
        public void should_send_message_on_proper_socket()
        {
            var endpoint = "endpoint";
            var endpoint2 = "endpoint2";
            BlockingCollection<ITransportMessage> sendQueueForFakeCommand = null;
            BlockingCollection<ITransportMessage> sendQueueForFakeCommand2 = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint))
                              .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string>((x, y, z) => sendQueueForFakeCommand = x);
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint2))
                  .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string>((x, y, z) => sendQueueForFakeCommand2 = x);
            _transport.Initialize();
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand2>(endpoint2));

            var command1 = new FakeCommand(2);
            var transportMessage = GetTransportMessage(command1);
            _transport.SendMessage(transportMessage);
            var command2 = new FakeCommand2(3);
            var transportMessage2 = GetTransportMessage(command2);
            _transport.SendMessage(transportMessage2);

            var fakeCommand = sendQueueForFakeCommand.Take();
            var fakeCommand2 = sendQueueForFakeCommand2.Take();
            Assert.AreEqual(typeof(FakeCommand).FullName, fakeCommand.MessageType);
            Assert.AreEqual(typeof(FakeCommand2).FullName, fakeCommand2.MessageType);
        }

        [Test]
        public void should_create_request_socket_for_endpoint_only_once_for_same_endpoint()
        {
            _transport.Initialize();
            var endpoint = "endpoint";

            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand2>(endpoint));
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));

            _transport.SendMessage(new TransportMessage(Guid.NewGuid(), new byte[0], typeof(FakeCommand).FullName, new byte[0] ));
            _transport.SendMessage(new TransportMessage(Guid.NewGuid(), new byte[0], typeof(FakeCommand2).FullName, new byte[0] ));

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                                                 It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()), Times.Exactly(1));
        }


        [Test]
        public void should_publish_events()
        {
            BlockingCollection<ITransportMessage> messagesToSend = null;
            _socketManagerMock.Setup(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()))
                                           .Callback<BlockingCollection<ITransportMessage>, string>((x, y) => messagesToSend = x);
            _transport.Initialize();
            var fakeEvent = new FakeEvent(2);
            var transportMessage = GetTransportMessage(fakeEvent, null);
           
            _transport.PublishMessage(transportMessage);

            var sentMessage = messagesToSend.Take();
            Assert.AreEqual(typeof(FakeEvent).FullName, sentMessage.MessageType);
            Assert.AreEqual(null, sentMessage.SendingSocketId);
            Assert.AreEqual(2, Serializer.Deserialize<FakeEvent>(sentMessage.Data).Test);
        }

        [Test]
        public void should_register_publisher()
        {
            _transport.Initialize();
            string endpoint = "endpoint";

            _transport.RegisterPeer(TestData.CreatePeerThatPublishes<FakeEvent>(endpoint));

            _socketManagerMock.Verify(x => x.SubscribeTo(endpoint, typeof(FakeEvent).FullName));
        }

        [Test, Timeout(1000)]
        public void should_raise_message_received_from_event()
        {
            AutoResetEvent waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.Setup(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>()))
                                         .Callback<BlockingCollection<ITransportMessage>>((x) => messagesReceived = x);
            var messageIdentity = Guid.NewGuid();
            var transportMessage = new TransportMessage(messageIdentity, null, typeof(FakeEvent).FullName, Serializer.Serialize(new FakeEvent(2)));
            _transport.Initialize();
            _transport.OnMessageReceived += (message) =>
                                                {
                                                    Assert.AreEqual(transportMessage.Data, message.Data);
                                                    Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                                                    Assert.AreEqual(transportMessage.SendingSocketId, message.SendingSocketId);
                                                    Assert.AreEqual(transportMessage.MessageIdentity, messageIdentity);
                                                    waitForEvent.Set();
                                                };
            string endpoint = "endpoint";
            _transport.RegisterPeer(TestData.CreatePeerThatPublishes<FakeEvent>(endpoint));
            messagesReceived.Add(transportMessage);

            waitForEvent.WaitOne();

        }

        [Test]
        public void should_raise_message_received_from_receivedAck()
        {
            var waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.CaptureVariable(() => messagesReceived, (s, x) => s.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), x, It.IsAny<string>()));
            var transportMessage = new TransportMessage(Guid.NewGuid(), null, typeof(FakeEvent).FullName, Serializer.Serialize(new FakeEvent(2)));
            _transport.Initialize();
            _transport.OnMessageReceived += (message) =>
            {
                Assert.AreEqual(transportMessage.Data, message.Data);
                Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                Assert.AreEqual(transportMessage.SendingSocketId, message.SendingSocketId);
                Assert.AreEqual(transportMessage.MessageIdentity, message.MessageIdentity);
                waitForEvent.Set();
            };
            string endpoint = "endpoint";
            _transport.RegisterPeer(TestData.CreatePeerThatHandles<FakeCommand>(endpoint));
            _transport.SendMessage(TestData.GenerateDummyMessage<FakeCommand>());
            messagesReceived.Add(transportMessage);

            waitForEvent.WaitOne();

        }



        [Test]
        public void should_dispose_socket_manager()
        {
            _transport.Initialize();

            _transport.Dispose();

            _socketManagerMock.Verify(x => x.Stop());
        }

        [TearDown]
        public void teardown()
        {
            _transport.Dispose();
        }
    }
}
