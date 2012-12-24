using System;
using Moq;
using NUnit.Framework;
using PersistenceService.Commands;
using ProtoBuf;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;
using Serializer = Shared.Serializer;

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
        private Mock<IPersistenceSynchronizer> _persistenceSyncMock;

        [SetUp]
        public void setup()
        {
            _factoryMock = new Mock<IReliabilityStrategyFactory>();
            _stratMock = new Mock<IStartupReliabilityStrategy>();
            _persistenceSyncMock = new Mock<IPersistenceSynchronizer>();
            _optionsRepoMock = new Mock<IMessageOptionsRepository>();
            _optionsRepoMock.Setup(x => x.GetOptionsFor(It.IsAny<string>())).Returns<string>(
                x => new MessageOptions(x, new ReliabilityInfo(ReliabilityLevel.FireAndForget, "broker")));
            _factoryMock.Setup(x => x.GetStartupStrategy(It.IsAny<MessageOptions>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IPersistenceSynchronizer>())).Returns(_stratMock.Object);
            _manager = new StartupStrategyManager(_factoryMock.Object, _optionsRepoMock.Object, _persistenceSyncMock.Object);
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

        [Test]
        public void should_handle_process_message_command()
        {
            var message = new ProcessMessagesCommand(typeof(FakeEvent).FullName, "Peer", new[] { TestData.GenerateDummyReceivedMessage<FakeEvent>(), TestData.GenerateDummyReceivedMessage<FakeEvent>() }, false);

            _manager.CheckMessage(new ReceivedTransportMessage(typeof(ProcessMessagesCommand).FullName, "Broker", Guid.NewGuid(), Serializer.Serialize(message)));

            _optionsRepoMock.Verify(x => x.GetOptionsFor(typeof(FakeEvent).FullName), Times.Exactly(1));
            _stratMock.Verify(x => x.GetMessagesToBubbleUp(It.Is<ReceivedTransportMessage>(y => y.MessageType == typeof(FakeEvent).FullName)), Times.Exactly(2));
            _stratMock.Verify(x => x.SetEndOfBrokerQueue(), Times.Never());

        }

        [Test]
        public void should_set_init_to_true_when_end_of_broker_queue()
        {
            var message = new ProcessMessagesCommand(typeof(FakeEvent).FullName, "Peer", new[] { TestData.GenerateDummyReceivedMessage<FakeEvent>(), TestData.GenerateDummyReceivedMessage<FakeEvent>() }, true);

            _manager.CheckMessage(new ReceivedTransportMessage(typeof(ProcessMessagesCommand).FullName, "Broker", Guid.NewGuid(), Serializer.Serialize(message)));

            _optionsRepoMock.Verify(x => x.GetOptionsFor(typeof(FakeEvent).FullName), Times.Exactly(1));
            _stratMock.Verify(x => x.GetMessagesToBubbleUp(It.Is<ReceivedTransportMessage>(y => y.MessageType == typeof(FakeEvent).FullName)), Times.Exactly(2));
            _stratMock.Verify(x => x.SetEndOfBrokerQueue(), Times.Once());

        }
    }
}