using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using Shared;
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
        private Mock<IReliabilityStrategy> _reliabilityStrategyMock;
        private Mock<IReliabilityStrategyFactory> _reliabilityStrategyFactoryMock;
        private Mock<IEndpointManager> _transportMock;

        [SetUp]
        public void setup()
        {
            _transportMock = new Mock<IEndpointManager>();
            _reliabilityStrategyFactoryMock = new Mock<IReliabilityStrategyFactory>();
            _reliabilityStrategyMock = new Mock<IReliabilityStrategy>();
            _reliabilityStrategyFactoryMock.Setup(x => x.GetStrategy(It.IsAny<MessageOptions>())).Returns(_reliabilityStrategyMock.Object);
            _reliabilityLayer = new ReliabilityLayer(_reliabilityStrategyFactoryMock.Object, _transportMock.Object);
        }

        [Test]
        public void should_use_registered_reliability_strategy_for_message_and_send_on_it()
        {
            var messageOption = new MessageOptions(ReliabilityLevel.FireAndForget, null);
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(messageOption);

            var transportMessage = new TransportMessage(typeof(FakeMessage).FullName, "", Guid.NewGuid(), new byte[0]);
            _reliabilityLayer.Send(transportMessage);

            _reliabilityStrategyFactoryMock.Verify(x => x.GetStrategy(messageOption));
            _reliabilityStrategyMock.Verify(x => x.SendOn(_transportMock.Object, transportMessage));
        }

        [Test]
        public void should_update_reliability_strategy_when_ack_arrives()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(new MessageOptions(ReliabilityLevel.FireAndForget, "TestBrokerId"));
            var sentMessage = new TransportMessage(typeof(FakeMessage).FullName, "", Guid.NewGuid(), new byte[0]);
            _reliabilityLayer.Send(sentMessage);

            var transportMessage = new TransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", sentMessage.MessageIdentity, new byte[0]);
            _transportMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);

            _reliabilityStrategyMock.Verify(x => x.CheckMessage(transportMessage));

        }

        [Test, Timeout(1000)]
        public void should_raise_message_received()
        {
            ITransportMessage capturedMessage = null;
             AutoResetEvent waitForProcessing = new AutoResetEvent(false);
             _reliabilityLayer.OnMessageReceived += x => { capturedMessage = x;
                                                             waitForProcessing.Set();
             };

            var transportMessage = new TransportMessage(typeof(ReceivedOnTransportAcknowledgement).FullName, "DO", Guid.NewGuid(), new byte[0]);
            _transportMock.Raise(x => x.OnMessageReceived += OnMessageReceived, transportMessage);
            waitForProcessing.WaitOne();
            Assert.AreEqual(transportMessage, capturedMessage);

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