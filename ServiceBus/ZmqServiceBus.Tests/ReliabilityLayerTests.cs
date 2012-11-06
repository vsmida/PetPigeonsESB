using System;
using System.Collections.Generic;
using System.Threading;
using DirectoryService.Commands;
using Moq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Tests.Transport;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class ReliabilityLayerTests
    {
        private class FakeMessage : IMessage
        {

        }

        private ReliabilityLayer _reliabilityLayer;
        private Mock<ISendingReliabilityStrategy> _reliabilityStrategyMock;
        private Mock<IStartupReliabilityStrategy> _startupStrategyMock;
        private Mock<IReliabilityStrategyFactory> _reliabilityStrategyFactoryMock;
        private Mock<IEndpointManager> _endpointManagerMock;

        [SetUp]
        public void setup()
        {
            _endpointManagerMock = new Mock<IEndpointManager>();
            _reliabilityStrategyFactoryMock = new Mock<IReliabilityStrategyFactory>();
            _reliabilityStrategyMock = new Mock<ISendingReliabilityStrategy>();
            _startupStrategyMock = new Mock<IStartupReliabilityStrategy>();
            _reliabilityStrategyFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(_reliabilityStrategyMock.Object);
            _reliabilityStrategyFactoryMock.Setup(x => x.GetStartupStrategy(It.IsAny<MessageOptions>(), It.IsAny<string>(), It.IsAny<string>())).Returns(_startupStrategyMock.Object);
            _startupStrategyMock.Setup(x => x.GetMessagesToBubbleUp(It.IsAny<TransportMessage>())).Returns
                <ITransportMessage>(x => new List<ITransportMessage> { x });
            _reliabilityLayer = new ReliabilityLayer(_reliabilityStrategyFactoryMock.Object, _endpointManagerMock.Object);
        }

        [Test]
        public void should_use_registered_reliability_strategy_for_message_and_send_on_it()
        {
            var messageOption = new MessageOptions(ReliabilityLevel.FireAndForget, null);
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(messageOption);

            var transportMessage = new TransportMessage(typeof(FakeMessage).FullName, "", Guid.NewGuid(), new byte[0]);
            _reliabilityLayer.Send(transportMessage);

            _reliabilityStrategyFactoryMock.Verify(x => x.GetSendingStrategy(messageOption));
            _reliabilityStrategyMock.Verify(x => x.SendOn(_endpointManagerMock.Object, transportMessage));
        }

        [Test]
        public void should_only_allow_messages_checked_by_startup_strategy_to_bubble_up()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, null));
            ITransportMessage capturedMessage = null;
            AutoResetEvent waitForProcessing = new AutoResetEvent(false);
            _reliabilityLayer.OnMessageReceived += x =>
            {
                capturedMessage = x;
                waitForProcessing.Set();
            };
            var otherMessage = TestData.GenerateDummyMessage<FakeMessage>();
            _startupStrategyMock.Setup(x => x.GetMessagesToBubbleUp(It.IsAny<TransportMessage>())).Returns(new List<ITransportMessage> { otherMessage });
            var sentMessage = TestData.GenerateDummyMessage<FakeMessage>();

            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, sentMessage);

            waitForProcessing.WaitOne();
            Assert.AreEqual(otherMessage, capturedMessage);
        }

        [Test]
        public void should_update_reliability_strategy_when_ack_arrives()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, "TestBrokerId"));
            var sentMessage = TestData.GenerateDummyMessage<FakeMessage>();
            _reliabilityLayer.Send(sentMessage);

            var transportMessage = new TransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", sentMessage.MessageIdentity, new byte[0]);
            _endpointManagerMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);

            _reliabilityStrategyMock.Verify(x => x.CheckMessage(transportMessage));

        }

        [Test, Timeout(1000)]
        public void should_raise_message_received()
        {
            ITransportMessage capturedMessage = null;
            AutoResetEvent waitForProcessing = new AutoResetEvent(false);
            _reliabilityLayer.OnMessageReceived += x =>
            {
                capturedMessage = x;
                waitForProcessing.Set();
            };

            var transportMessage = new TransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", Guid.NewGuid(), new byte[0]);
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

        [Test]
        public void should_register_reliability_of_special_infrastructure_messages_on_start()
        {
            _reliabilityLayer.Initialize();
            
            Assert.DoesNotThrow(() => _reliabilityLayer.Send(TestData.GenerateDummyMessage<ReceivedOnTransportAcknowledgement>()));
            Assert.DoesNotThrow(() => _reliabilityLayer.Send(TestData.GenerateDummyMessage<InitializeTopologyAndMessageSettings>()));
            Assert.DoesNotThrow(() => _reliabilityLayer.Send(TestData.GenerateDummyMessage<RegisterPeerCommand>()));
        }

        //[Test, Timeout(100000)]
        //public void should__not_block_while_raising_message_received()
        //{
        //    ITransportMessage capturedMessage = null;
        //    AutoResetEvent waitForProcessing = new AutoResetEvent(false);
        //    AutoResetEvent waitForThreadToWork = new AutoResetEvent(false);
        //    new BackgroundThread(() =>
        //                             {
        //                                 _reliabilityLayer.OnMessageReceived += x =>
        //                                                                            {
        //                                                                                waitForProcessing.WaitOne();
        //                                                                                if (capturedMessage == null)
        //                                                                                    capturedMessage = x;
        //                                                                                waitForThreadToWork.Set();
        //                                                                            };
        //                             }).Start();


        //var sentMessage = new TransportMessage(typeof (FakeMessage).FullName, "", Guid.NewGuid(), new byte[0]);
        //    var transportMessage = new TransportMessage(typeof (ReceivedOnTransportAcknowledgement).FullName, "DO",
        //                                                sentMessage.MessageIdentity, new byte[0]);
        //    _transportMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);
        //    Assert.IsNull(capturedMessage);
        //    _reliabilityStrategyMock.Setup(x => x.CheckMessage(It.IsAny<ITransportMessage>())).Callback
        //        <ITransportMessage>(x => waitForProcessing.Set());
        //    waitForThreadToWork.WaitOne();
        //    Assert.AreEqual(sentMessage, capturedMessage);
        //}

        private void OnMessageReceived(ITransportMessage obj)
        {

        }
    }
}