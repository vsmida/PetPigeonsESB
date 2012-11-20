using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class StartupStrategyManagerTests
    {
        [ProtoContract]
        private class FakeEvent : IEvent
        {
            
        }

        private StartupStrategyManager _manager;
        private Mock<IReliabilityStrategyFactory> _factoryMock;
        private Mock<IStartupReliabilityStrategy> _stratMock;
        private Mock<IMessageOptionsRepository> _optionsRepoMock;

        [SetUp]
        public void setup()
        {
            _factoryMock = new Mock<IReliabilityStrategyFactory>();
            _stratMock = new Mock<IStartupReliabilityStrategy>();
            _optionsRepoMock = new Mock<IMessageOptionsRepository>();
            _optionsRepoMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns<string>(
                x => new MessageOptions(x, ReliabilityLevel.FireAndForget, "broker"));
            _factoryMock.Setup(
                x => x.GetStartupStrategy(It.IsAny<MessageOptions>(), It.IsAny<string>(), It.IsAny<string>())).Returns(
                    _stratMock.Object);
            _manager = new StartupStrategyManager(_factoryMock.Object, _optionsRepoMock.Object);
        }

        [Test]
        public void should_create_right_strategy_when_right_one_unknown()
        {
            var message = TestData.GenerateDummyReceivedMessage<FakeEvent>();
            _manager.CheckMessage(message);

            _optionsRepoMock.Verify(x => x.GetOptionsFor(message.MessageType));
            _stratMock.Verify(x => x.GetMessagesToBubbleUp(message));
        }


        [Test]
        public void should_use_right_strategy_when_right_one_known()
        {
            var message = TestData.GenerateDummyReceivedMessage<FakeEvent>();
            _manager.CheckMessage(message);
            
            _manager.CheckMessage(message);

            _optionsRepoMock.Verify(x => x.GetOptionsFor(message.MessageType), Times.Exactly(1));
            _stratMock.Verify(x => x.GetMessagesToBubbleUp(message), Times.Exactly(2));
        }
    }
}