using System;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
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
        private Mock<IMessageOptionsRepository> _optionsRepositoryMock;
        private Mock<IReliabilityStrategyFactory> _reliabilityStratFactoryMock;
        private Mock<ICallbackRepository> _callbackManagerMock = new Mock<ICallbackRepository>();
        private Mock<IPeerManager> _peerManagerMock;

        [SetUp]
        public void setup()
        {
            _callbackManagerMock = new Mock<ICallbackRepository>();
            _optionsRepositoryMock = new Mock<IMessageOptionsRepository>();
            _reliabilityStratFactoryMock = new Mock<IReliabilityStrategyFactory>();
         //   _messageSender = new MessageSender(_optionsRepositoryMock.Object, _reliabilityStratFactoryMock.Object, _callbackManagerMock.Object, _peerManagerMock.Object);
        }

        [Test]
        public void should_register_default_callback_when_none_supplied()
        {
            var stratMock = new Mock<ISendingReliabilityStrategy>();
            _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
                stratMock.Object);
            _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("", new ReliabilityInfo(ReliabilityLevel.SendToClientAndBrokerNoAck, "")));

            var blockableUntilCompletion = _messageSender.Send(new FakeCommand());

           _callbackManagerMock.Verify(x => x.RegisterCallback(It.IsAny<Guid>(),
               It.Is<DefaultCompletionCallback>(y => y!= null && y == blockableUntilCompletion )));

        }

        [Test]
        public void should_register_callback_with_manager()
        {
            var stratMock = new Mock<ISendingReliabilityStrategy>();
            _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
                stratMock.Object);
            _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("", new ReliabilityInfo(ReliabilityLevel.SendToClientAndBrokerNoAck, "")));

            var defaultCompletionCallback = new DefaultCompletionCallback();
            var blockableUntilCompletion = _messageSender.Send(new FakeCommand(), defaultCompletionCallback);

            _callbackManagerMock.Verify(x => x.RegisterCallback(It.IsAny<Guid>(), defaultCompletionCallback));
            Assert.AreEqual(defaultCompletionCallback, blockableUntilCompletion);
        }

        //[Test]
        //public void should_get_strategy_and_send_on_it()
        //{
        //    var stratMock = new Mock<ISendingReliabilityStrategy>();
        //    _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
        //        stratMock.Object);
        //    _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("",new ReliabilityInfo(ReliabilityLevel.SendToClientAndBrokerNoAck,"")));

        //    _messageSender.Send(new FakeCommand());

        //    stratMock.Verify(x => x.Send(It.Is<SendingBusMessage>(y => y.MessageType == typeof(FakeCommand).FullName)));
        //}

        //[Test]
        //public void should_get_strategy_and_route_on_it()
        //{
        //    var stratMock = new Mock<ISendingReliabilityStrategy>();
        //    _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
        //        stratMock.Object);
        //    _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("",new ReliabilityInfo( ReliabilityLevel.SendToClientAndBrokerNoAck, "")));

        //    _messageSender.Route(new FakeCommand(),"Test");

        //    stratMock.Verify(x => x.RouteOn(_endpointManagerMock.Object, It.Is<SendingBusMessage>(y => y.MessageType == typeof(FakeCommand).FullName), "Test"));
        //}

        //[Test]
        //public void should_get_strategy_and_publish_on_it()
        //{
        //    var stratMock = new Mock<ISendingReliabilityStrategy>();
        //    _reliabilityStratFactoryMock.Setup(x => x.GetSendingStrategy(It.IsAny<MessageOptions>())).Returns(
        //        stratMock.Object);
        //    _optionsRepositoryMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns(new MessageOptions("", new ReliabilityInfo(ReliabilityLevel.SendToClientAndBrokerNoAck, "")));

        //    _messageSender.Publish(new FakeEvent());

        //    stratMock.Verify(x => x.PublishOn(_endpointManagerMock.Object, It.Is<SendingBusMessage>(y => y.MessageType == typeof(FakeEvent).FullName)));
        //}
    }
}