using System;
using System.Collections.Generic;
using System.Linq;
using Bus;
using Bus.Attributes;
using Bus.Dispatch;
using Bus.DisruptorEventHandlers;
using Bus.MessageInterfaces;
using Bus.Subscriptions;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using Moq;
using NUnit.Framework;
using Shared;
using Tests.Transport;

namespace Tests.OutboundPath
{
    [TestFixture]
    public class MessageTargetHandlerTests
    {
        [SubscriptionFilterAttributeActive(false)]
        private class CannotPassFilter : ISubscriptionFilter<TestData.FakeCommand>
        {
            public bool Matches(IMessage item)
            {
                return false;
            }
        }

        private Mock<IPeerManager> _peerManagerMock;
        private Mock<IAssemblyScanner> _assemblyScannerMock;
        private Mock<IPeerConfiguration> _peerConfigurationMock;
        private Mock<ICallbackRepository> _callbackRepositoryMock;
        private Mock<IReliabilityCoordinator> _reliabilityCoordinatorMock;
        private ServicePeer _otherPeer = new ServicePeer("S1", new List<MessageSubscription> { new MessageSubscription(typeof(TestData.FakeCommand), "S1", new ZmqEndpoint("toto"), null, ReliabilityLevel.Persisted) }, null);
        private MessageTargetsHandler _messageTargetsHandler;
        private MessageTargetHandlerData _messageTargetHandlerData = new MessageTargetHandlerData() { Callback = null, IsAcknowledgement = false, Message = new TestData.FakeCommand(), TargetPeer = null };

        [SetUp]
        public void setup()
        {
            _assemblyScannerMock = new Mock<IAssemblyScanner>();
            _assemblyScannerMock.Setup(x => x.FindMessageSerializers(null)).Returns(new Dictionary<Type, Type>
                                                                                    {
                                                                                        {
                                                                                            typeof (TestData.FakeCommand),
                                                                                            typeof (
                                                                                            TestData.
                                                                                            FakeCommandSerializer)
                                                                                        }
                                                                                    });
            _peerManagerMock = new Mock<IPeerManager>();
            _peerManagerMock.Setup(x => x.GetAllSubscriptions()).Returns(() => new Dictionary<string, List<MessageSubscription>>
                                                                             {
                                                                                 {
                                                                                     typeof (TestData.FakeCommand).FullName
                                                                                     , new List<MessageSubscription>{_otherPeer.HandledMessages.First()}
                                                                                 }
                                                                             });
            _peerConfigurationMock = new Mock<IPeerConfiguration>();
            _peerConfigurationMock.SetupGet(x => x.PeerName).Returns("Me");
            _callbackRepositoryMock = new Mock<ICallbackRepository>();
            _reliabilityCoordinatorMock = new Mock<IReliabilityCoordinator>();
            _messageTargetsHandler = new MessageTargetsHandler(_callbackRepositoryMock.Object, _peerManagerMock.Object, _peerConfigurationMock.Object, _reliabilityCoordinatorMock.Object, _assemblyScannerMock.Object);
            _peerManagerMock.Raise(x => x.PeerConnected += OnPeerConnected, _otherPeer);

        }

        [Test]
        public void should_use_custom_serializer_if_available()
        {

            bool serializeCalled = false;
            TestData.FakeCommandSerializer.SerializeCalled += x => serializeCalled = true;
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            Assert.IsTrue(serializeCalled);

        }

        [Test]
        public void should_send_to_the_peers_with_a_subscription()
        {
            var wrongPeer = new ServicePeer("S2", new List<MessageSubscription> { new MessageSubscription(typeof(TestData.FakeEvent), "S1", new ZmqEndpoint("toto"), null, ReliabilityLevel.Persisted) }, null);
            _peerManagerMock.Raise(x => x.PeerConnected += OnPeerConnected, wrongPeer);
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            Assert.IsNull(outboundDisruptorEntry.NetworkSenderData.Command);
            Assert.AreEqual(1, outboundDisruptorEntry.NetworkSenderData.WireMessages.Count);
            Assert.AreEqual(_otherPeer.HandledMessages[0].Endpoint, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].Endpoint);
            Assert.AreEqual(null, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SequenceNumber);
            Assert.AreEqual(_peerConfigurationMock.Object.PeerName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SendingPeer);
            Assert.AreEqual(typeof(TestData.FakeCommand).FullName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageType);
            Assert.AreEqual(BusSerializer.Serialize(new TestData.FakeCommand()), outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.Data);
            Assert.AreNotEqual(Guid.Empty, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageIdentity);

        }

        [Test]
        public void should_register_callback()
        {
            _messageTargetHandlerData.Callback = new DefaultCompletionCallback();
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            _callbackRepositoryMock.Verify(x => x.RegisterCallback(It.Is<Guid>(y => y!=Guid.Empty), _messageTargetHandlerData.Callback));
        }

        [Test]
        public void should_ensure_reliability_constraints_by_sending_shadow_messages_through_coordinator()
        {
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            _reliabilityCoordinatorMock.Verify(x => x.EnsureReliability(outboundDisruptorEntry, outboundDisruptorEntry.MessageTargetHandlerData.Message,
                It.Is<IEnumerable<MessageSubscription>>(y => y.Contains(_otherPeer.HandledMessages[0]) && y.Count() == 1), It.IsAny<MessageWireData>()));
        }

        [Test]
        public void should_filter_subscriptions()
        {
            var wrongPeer = new ServicePeer("S2", new List<MessageSubscription> { new MessageSubscription(typeof(TestData.FakeCommand), "S1", new ZmqEndpoint("toto"), new CannotPassFilter(), ReliabilityLevel.Persisted) }, null);
            _peerManagerMock.Raise(x => x.PeerConnected += OnPeerConnected, wrongPeer);
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            Assert.IsNull(outboundDisruptorEntry.NetworkSenderData.Command);
            Assert.AreEqual(1, outboundDisruptorEntry.NetworkSenderData.WireMessages.Count);
            Assert.AreEqual(_otherPeer.HandledMessages[0].Endpoint, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].Endpoint);
            Assert.AreEqual(null, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SequenceNumber);
            Assert.AreEqual(_peerConfigurationMock.Object.PeerName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SendingPeer);
            Assert.AreEqual(typeof(TestData.FakeCommand).FullName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageType);
            Assert.AreEqual(BusSerializer.Serialize(new TestData.FakeCommand()), outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.Data);
            Assert.AreNotEqual(Guid.Empty, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageIdentity);
        }

        [Test]
        public void should_listen_to_peer_connected_and_send()
        {
            var outboundDisruptorEntry = new OutboundDisruptorEntry() { MessageTargetHandlerData = _messageTargetHandlerData };

            _messageTargetsHandler.OnNext(outboundDisruptorEntry, 0, true);

            Assert.IsNull(outboundDisruptorEntry.NetworkSenderData.Command);
            Assert.AreEqual(1, outboundDisruptorEntry.NetworkSenderData.WireMessages.Count);
            Assert.AreEqual(_otherPeer.HandledMessages[0].Endpoint, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].Endpoint);
            Assert.AreEqual(null, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SequenceNumber);
            Assert.AreEqual(_peerConfigurationMock.Object.PeerName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.SendingPeer);
            Assert.AreEqual(typeof(TestData.FakeCommand).FullName, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageType);
            Assert.AreEqual(BusSerializer.Serialize(new TestData.FakeCommand()), outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.Data);
            Assert.AreNotEqual(Guid.Empty, outboundDisruptorEntry.NetworkSenderData.WireMessages[0].MessageData.MessageIdentity);
        }

        private void OnPeerConnected(ServicePeer obj)
        {

        }
    }
}