using System;
using System.Collections.Concurrent;
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
            _socketManagerMock.Verify(x => x.CreateResponseSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.Identity));
        }

        [Test]
        public void should_create_a_socket_to_handle_outgoing_events_on_init()
        {
            _transport.Initialize();

            var endpoint = _configuration.EventsProtocol + "://*:" + _configuration.EventsPort;
            _socketManagerMock.Verify(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint));
        }

        [Test]
        public void should_register_command_handling_endpoint_and_send()
        {
            BlockingCollection<ITransportMessage> sendQueue = null;
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(),
                                      _configuration.Identity))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>(
                    (x, y, z, t) => sendQueue = x);

            _transport.Initialize();
            var endpoint = "endpoint";
            _transport.RegisterCommandHandlerEndpoint<FakeCommand>(endpoint);

            _socketManagerMock.VerifyAll();

            _transport.SendMessage(new FakeCommand(2));

            var sentTransportMessage = sendQueue.Take();
            Assert.AreEqual(_configuration.Identity, sentTransportMessage.SenderIdentity);
            Assert.AreEqual(typeof(FakeCommand).FullName, sentTransportMessage.MessageType);
            Assert.AreEqual(2, Serializer.Deserialize<FakeCommand>(sentTransportMessage.Data).Test);
        }
        [Test]
        public void should_ack_message()
        {
            BlockingCollection<ITransportMessage> ackQueue = null;
            _socketManagerMock.Setup(x => x.CreateResponseSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                        It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(),
                                      _configuration.Identity))
                .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>(
                    (x, y, z, t) => ackQueue = y);
            _transport.Initialize();

            var messageId = Guid.NewGuid();
            var recipientIdentity = "toto";
            _transport.AckMessage(recipientIdentity, messageId, true);

           Assert.AreEqual(1, ackQueue.Count);
            var transportMessage = ackQueue.Take();
            Assert.AreNotEqual(Guid.Empty, transportMessage.MessageIdentity);
            Assert.AreEqual(recipientIdentity, transportMessage.SenderIdentity);
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
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint, _configuration.Identity))
                              .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>((x, y, z, t) => sendQueueForFakeCommand = x);
            _socketManagerMock.Setup(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint2, _configuration.Identity))
                  .Callback<BlockingCollection<ITransportMessage>, BlockingCollection<ITransportMessage>, string, string>((x, y, z, t) => sendQueueForFakeCommand2 = x);
            _transport.Initialize();
            _transport.RegisterCommandHandlerEndpoint<FakeCommand>(endpoint);
            _transport.RegisterCommandHandlerEndpoint<FakeCommand2>(endpoint2);

            _transport.SendMessage(new FakeCommand(2));
            _transport.SendMessage(new FakeCommand2(3));

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
            _transport.RegisterCommandHandlerEndpoint<FakeCommand>(endpoint);
            _transport.RegisterCommandHandlerEndpoint<FakeCommand2>(endpoint);
            _transport.RegisterCommandHandlerEndpoint<FakeCommand>(endpoint);

            _socketManagerMock.Verify(x => x.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(),
                                                                 It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Test]
        public void should_publish_events()
        {
            BlockingCollection<ITransportMessage> messagesToSend = null;
            _socketManagerMock.Setup(x => x.CreatePublisherSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()))
                                           .Callback<BlockingCollection<ITransportMessage>, string>((x, y) => messagesToSend = x);
            _transport.Initialize();
            _transport.PublishMessage(new FakeEvent(2));

            var sentMessage = messagesToSend.Take();
            Assert.AreEqual(typeof(FakeEvent).FullName, sentMessage.MessageType);
            Assert.AreEqual(null, sentMessage.SenderIdentity);
            Assert.AreEqual(2, Serializer.Deserialize<FakeEvent>(sentMessage.Data).Test);
        }

        [Test]
        public void should_register_publisher()
        {
            _transport.Initialize();
            string endpoint = "endpoint";

            _transport.RegisterPublisherEndpoint<FakeEvent>(endpoint);

            _socketManagerMock.Verify(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), endpoint));
        }

        [Test, Timeout(1000)]
        public void should_raise_message_received_from_event()
        {
            AutoResetEvent waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.Setup(x => x.CreateSubscribeSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), It.IsAny<string>()))
                                         .Callback<BlockingCollection<ITransportMessage>, string>((x, y) => messagesReceived = x);
            var messageIdentity = Guid.NewGuid();
            var transportMessage = new TransportMessage(messageIdentity,null, typeof (FakeEvent).FullName, Serializer.Serialize(new FakeEvent(2)));
            _transport.Initialize();
            _transport.OnMessageReceived += (message) =>
                                                {
                                                    Assert.AreEqual(transportMessage.Data, message.Data);
                                                    Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                                                    Assert.AreEqual(transportMessage.SenderIdentity, message.SenderIdentity);
                                                    Assert.AreEqual(transportMessage.MessageIdentity, messageIdentity);
                                                    waitForEvent.Set();
                                                };
            string endpoint = "endpoint";
            _transport.RegisterPublisherEndpoint<FakeEvent>(endpoint);
            messagesReceived.Add(transportMessage);

            waitForEvent.WaitOne();

        }

        [Test]
        public void should_raise_message_received_from_receivedAck()
        {
            var waitForEvent = new AutoResetEvent(false);
            BlockingCollection<ITransportMessage> messagesReceived = null;
            _socketManagerMock.CaptureVariable(() => messagesReceived, (s, x) => s.CreateRequestSocket(It.IsAny<BlockingCollection<ITransportMessage>>(), x, It.IsAny<string>(), It.IsAny<string>()));
            var transportMessage = new TransportMessage(Guid.NewGuid(), null, typeof(FakeEvent).FullName, Serializer.Serialize(new FakeEvent(2)));
            _transport.Initialize();
            _transport.OnMessageReceived += (message) =>
            {
                Assert.AreEqual(transportMessage.Data, message.Data);
                Assert.AreEqual(transportMessage.MessageType, message.MessageType);
                Assert.AreEqual(transportMessage.SenderIdentity, message.SenderIdentity);
                Assert.AreEqual(transportMessage.MessageIdentity, message.MessageIdentity);
                waitForEvent.Set();
            };
            string endpoint = "endpoint";
            _transport.RegisterCommandHandlerEndpoint<FakeCommand>(endpoint);
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
