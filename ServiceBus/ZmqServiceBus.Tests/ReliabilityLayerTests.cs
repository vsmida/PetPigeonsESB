using System;
using System.Collections.Generic;
using System.Threading;
using DirectoryService.Commands;
using Moq;
using NUnit.Framework;
using PersistenceService.Commands;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class ReliabilityLayerTests
    {
        private class FakeMessage : IMessage
        {

        }

        private ReliabilityLayer _reliabilityLayer;
        private Mock<ISendingReliabilityStrategy> _sendingReliabilityStrategyMock;
        private Mock<IStartupReliabilityStrategy> _startupStrategyMock;
        private Mock<IReliabilityStrategyFactory> _reliabilityStrategyFactoryMock;
        private Mock<IEndpointManager> _endpointManagerMock;
        private Mock<ISendingStrategyManager> _sendingStrategyManagerMock;

        [SetUp]
        public void setup()
        {
            _endpointManagerMock = new Mock<IEndpointManager>();
            _reliabilityStrategyFactoryMock = new Mock<IReliabilityStrategyFactory>();
            _sendingReliabilityStrategyMock = new Mock<ISendingReliabilityStrategy>();
            _startupStrategyMock = new Mock<IStartupReliabilityStrategy>();
            _reliabilityStrategyFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(_sendingReliabilityStrategyMock.Object);
            _reliabilityStrategyFactoryMock.Setup(x => x.GetStartupStrategy(It.IsAny<MessageOptions>(), It.IsAny<string>(), It.IsAny<string>())).Returns(_startupStrategyMock.Object);
            _startupStrategyMock.Setup(x => x.GetMessagesToBubbleUp(It.IsAny<ReceivedTransportMessage>())).Returns
                <IReceivedTransportMessage>(x => new List<IReceivedTransportMessage> { x });
            _sendingStrategyManagerMock = new Mock<ISendingStrategyManager>();
            _reliabilityLayer = new ReliabilityLayer(_reliabilityStrategyFactoryMock.Object, _endpointManagerMock.Object, _sendingStrategyManagerMock.Object);
        }

        [Test]
        public void should_use_registered_reliability_strategy_for_message_and_send_on_it()
        {
            var messageOption = new MessageOptions(ReliabilityLevel.FireAndForget, null);
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(messageOption);

            var transportMessage = new SendingTransportMessage(typeof(FakeMessage).FullName, Guid.NewGuid(), new byte[0]);
            _reliabilityLayer.Send(transportMessage);

            _reliabilityStrategyFactoryMock.Verify(x => x.GetSendingStrategy(messageOption));
            _sendingReliabilityStrategyMock.Verify(x => x.SendOn(_endpointManagerMock.Object, _sendingStrategyManagerMock.Object, transportMessage));
        }

        [Test]
        public void should_only_allow_messages_checked_by_startup_strategy_to_bubble_up()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, null));
            IReceivedTransportMessage capturedMessage = null;
            AutoResetEvent waitForProcessing = new AutoResetEvent(false);
            _reliabilityLayer.OnMessageReceived += x =>
            {
                capturedMessage = x;
                waitForProcessing.Set();
            };
            var otherMessage = TestData.GenerateDummyReceivedMessage<FakeMessage>();
            _startupStrategyMock.Setup(x => x.GetMessagesToBubbleUp(It.IsAny<ReceivedTransportMessage>())).Returns(new List<IReceivedTransportMessage> { otherMessage });
            var sentMessage = TestData.GenerateDummyReceivedMessage<FakeMessage>();

            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, sentMessage);

            waitForProcessing.WaitOne();
            Assert.AreEqual(otherMessage, capturedMessage);
        }

        [Test]
        public void should_update_reliability_strategy_when_ack_arrives()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, "TestBrokerId"));
            var sentMessage = TestData.GenerateDummySendingMessage<FakeMessage>();
            _reliabilityLayer.Send(sentMessage);

            var transportMessage = new ReceivedTransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", sentMessage.MessageIdentity, new byte[0]);
            _sendingStrategyManagerMock.Setup(x => x.GetSendingStrategy(transportMessage)).Returns(_sendingReliabilityStrategyMock.Object);
            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);



            _sendingReliabilityStrategyMock.Verify(x => x.CheckMessage(transportMessage));

        }

        [Test]
        public void should_not_let_transport_acks_bubble_up()
        {
            AutoResetEvent waitForOneMessageToBeProcessed = new AutoResetEvent(false);
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, "TestBrokerId"));
            bool messageReceivedRaised = false;
            _reliabilityLayer.OnMessageReceived += x =>
                                                       {
                                                           if(x.MessageType == typeof(ReceivedOnTransportAcknowledgement).FullName)
                                                           messageReceivedRaised = true;
                                                           waitForOneMessageToBeProcessed.Set();
                                                       };

            var sentMessage = TestData.GenerateDummySendingMessage<FakeMessage>();
            _reliabilityLayer.Send(sentMessage);

            var transportMessageTest = new ReceivedTransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", sentMessage.MessageIdentity, new byte[0]);
            var transportMessageBubble = new ReceivedTransportMessage(typeof(FakeMessage).FullName, "DO", sentMessage.MessageIdentity, new byte[0]);
            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessageTest);
            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessageBubble);

            waitForOneMessageToBeProcessed.WaitOne();
            Assert.IsFalse(messageReceivedRaised);
        }

        [Test, Timeout(1000)]
        public void should_raise_message_received()
        {
            IReceivedTransportMessage capturedMessage = null;
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, "TestBrokerId"));
            AutoResetEvent waitForProcessing = new AutoResetEvent(false);
            _reliabilityLayer.OnMessageReceived += x =>
            {
                capturedMessage = x;
                waitForProcessing.Set();
            };

            var transportMessage = new ReceivedTransportMessage(typeof(FakeMessage).FullName, "DO", Guid.NewGuid(), new byte[0]);
            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);
            waitForProcessing.WaitOne();
            Assert.AreEqual(transportMessage, capturedMessage);

        }

        [Test]
        public void should_dispose_transport()
        {
            _reliabilityLayer.Dispose();
            _endpointManagerMock.Verify(x => x.Dispose());
        }

        [Test]
        public void should_initialize_transport()
        {
            _reliabilityLayer.Initialize();
            _endpointManagerMock.Verify(x => x.Initialize());
        }


        private void OnMessageReceived(IReceivedTransportMessage obj)
        {

        }
    }
}