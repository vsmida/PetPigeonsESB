using System;
using Moq;
using NUnit.Framework;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStates;
using ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Tests.Transport
{
    [TestFixture]
    public class SendingStrategyStateManagerTests
    {
        private class FakeMessage : IMessage
        {

        }

        private SendingStrategyStateManager _manager;

        [SetUp]
        public void setup()
        {
            _manager = new SendingStrategyStateManager();
        }

        [Test]
        public void should_register_strategies_and_let_them_check_incoming_messages()
        {
            var state1Mock = new Mock<ISendingReliabilityStrategyState>();
            var state2Mock = new Mock<ISendingReliabilityStrategyState>();
            var id = Guid.NewGuid();
            state2Mock.SetupGet(x => x.SentMessageId).Returns(id); //avoid dictionary collision
           
            _manager.RegisterStrategy(state1Mock.Object);
            _manager.RegisterStrategy(state2Mock.Object);

            var transportMessage = TestData.GenerateDummyReceivedMessage<FakeMessage>(id);
            _manager.CheckMessage(transportMessage);

            state1Mock.Verify(x => x.CheckMessage(transportMessage));
            state2Mock.Verify(x => x.CheckMessage(transportMessage));

        }
    }
}