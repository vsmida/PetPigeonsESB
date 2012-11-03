using System;
using System.Text;
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
        private Mock<ITransport> _transportMock;

        [SetUp]
        public void setup()
        {
            _transportMock = new Mock<ITransport>();
            _reliabilityStrategyFactoryMock = new Mock<IReliabilityStrategyFactory>();
            _reliabilityStrategyMock = new Mock<IReliabilityStrategy>();
            _reliabilityStrategyFactoryMock.Setup(x => x.GetStrategy(It.IsAny<ReliabilityOption>())).Returns(_reliabilityStrategyMock.Object);
            _reliabilityLayer = new ReliabilityLayer(_reliabilityStrategyFactoryMock.Object, _transportMock.Object);
        }

        [Test]
        public void should_use_registered_reliability_strategy_for_message_and_wait_on_it()
        {
            var waitForStrategy = new AutoResetEvent(true);
            _reliabilityStrategyMock.SetupGet(x => x.WaitForReliabilityConditionsToBeFulfilled).Returns(waitForStrategy);
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(ReliabilityOption.FireAndForget);

            _reliabilityLayer.Send(new TransportMessage(Guid.NewGuid(), null, typeof(FakeMessage).FullName, new byte[0]));

            _reliabilityStrategyFactoryMock.Verify(x => x.GetStrategy(ReliabilityOption.FireAndForget));
            _reliabilityStrategyMock.VerifyGet(x => x.WaitForReliabilityConditionsToBeFulfilled);
        }

        [Test]
        public void should_update_reliability_strategy_when_client_ack_arrives()
        {
            _reliabilityLayer.RegisterMessageReliabilitySetting<FakeMessage>(ReliabilityOption.FireAndForget);
            var sentMessage = new TransportMessage(Guid.NewGuid(), null, typeof (FakeMessage).FullName, new byte[0]);
            _reliabilityLayer.Send(sentMessage);
            
            _transportMock.Raise(x => x.OnMessageReceived += OnMessageReceived, new TransportMessage(sentMessage.MessageIdentity,null,typeof(ReceivedOnTransportAcknowledgement).FullName, new byte[0]));

            Assert.IsTrue(_reliabilityStrategyMock.Object.ClientTransportAckReceived);
        }

        private void OnMessageReceived(ITransportMessage obj)
        {
                
        }
    }
}