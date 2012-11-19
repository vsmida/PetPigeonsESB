using System;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class MessageSenderTests
    {
        [ProtoContract]
        private class FakeCommand : ICommand
        {
            
        }

        [ProtoContract]
        private class FakeEvent : IEvent
        {

        }


        private MessageSender _messageSender;
        private Mock<IEndpointManager> _endpointManagerMock;
        private Mock<IMessageOptionsRepository> _optionsRepositoryMock;
        private Mock<IReliabilityStrategyFactory> _reliabilityStratFactoryMock;

        [SetUp]
        public void setup()
        {
            _endpointManagerMock = new Mock<IEndpointManager>();
            _optionsRepositoryMock = new Mock<IMessageOptionsRepository>();
            _reliabilityStratFactoryMock = new Mock<IReliabilityStrategyFactory>();
            _messageSender = new MessageSender(_endpointManagerMock.Object, _optionsRepositoryMock.Object, _reliabilityStratFactoryMock.Object);
        }

        [Test]
        public void should_get_strategy_and_send_on_it()
        {
            var stratMock = new Mock<ISendingReliabilityStrategy>();
            _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
                stratMock.Object);
            _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("",ReliabilityLevel.SendToClientAndBrokerNoAck,""));

            _messageSender.Send(new FakeCommand());

            stratMock.Verify(x => x.SendOn(_endpointManagerMock.Object, It.Is<SendingTransportMessage>(y => y.MessageType == typeof(FakeCommand).FullName)));
        }

        [Test]
        public void should_get_strategy_and_route_on_it()
        {
            var stratMock = new Mock<ISendingReliabilityStrategy>();
            _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
                stratMock.Object);
            _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("", ReliabilityLevel.SendToClientAndBrokerNoAck, ""));

            _messageSender.Route(new FakeCommand(),"Test");

            stratMock.Verify(x => x.RouteOn(_endpointManagerMock.Object, It.Is<SendingTransportMessage>(y => y.MessageType == typeof(FakeCommand).FullName), "Test"));
        }

        [Test]
        public void should_get_strategy_and_publish_on_it()
        {
            var stratMock = new Mock<ISendingReliabilityStrategy>();
            _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
                stratMock.Object);
            _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("", ReliabilityLevel.SendToClientAndBrokerNoAck, ""));

            _messageSender.Publish(new FakeEvent());

            stratMock.Verify(x => x.PublishOn(_endpointManagerMock.Object, It.Is<SendingTransportMessage>(y => y.MessageType == typeof(FakeEvent).FullName)));
        }
    }
}