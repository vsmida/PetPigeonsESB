using Moq;
using NUnit.Framework;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BrokerPersistenceSynchronizerTests
    {
        private InMemoryPersistenceSynchronizer _synchronizer;
        private Mock<IMessageSender> _messageSenderMock;
        [SetUp]
        public void setup()
        {
            _messageSenderMock = new Mock<IMessageSender>();
          //  _synchronizer = new BrokerPersistenceSynchronizer(_messageSenderMock.Object);
        }

        //[Test]
        //public void should_ask_broker_for_peer_synchronization_when_requested()
        //{
        //    SynchronizePeerMessageCommand command = null;
        //    _messageSenderMock.Setup(x => x.Send(It.IsAny<ICommand>(), It.IsAny<ICompletionCallback>())).Callback<ICommand, ICompletionCallback>(
        //        (x,y) => command = (SynchronizePeerMessageCommand) x);
        //    string eventType = null;
        //    string eventPeer = null;
        //    _synchronizer.MessageTypeForPeerSynchronizationRequested += (type, peer) =>
        //                                                                    {
        //                                                                        eventType = type;
        //                                                                        eventPeer = peer;
        //                                                                    };
            
        //    _synchronizer.SynchronizeMessageType("type", "peer");

        //    Assert.AreEqual("type", command.MessageType);
        //    Assert.AreEqual("peer", command.OriginatingPeer);
        //    Assert.AreEqual("type", eventType);
        //    Assert.AreEqual("peer", eventPeer);
        //}

        //[Test]
        //public void should_ask_broker_for_synchronization_when_requested()
        //{
        //    SynchronizeMessageCommand command = null;
        //    _messageSenderMock.Setup(x => x.Send(It.IsAny<ICommand>(), It.IsAny<ICompletionCallback>())).Callback<ICommand, ICompletionCallback>(
        //        (x,y) => command = (SynchronizeMessageCommand)x);
        //    string eventType = null;
        //    _synchronizer.MessageTypeSynchronizationRequested += (type) =>
        //    {
        //        eventType = type;
        //    };

        //    _synchronizer.SynchronizeMessageType("type");

        //    Assert.AreEqual("type", command.MessageType);
        //    Assert.AreEqual("type", eventType);
        //}
    }
}