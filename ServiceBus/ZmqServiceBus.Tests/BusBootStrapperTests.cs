using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Dispatch;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;
using ZmqServiceBus.Contracts;
using ZmqServiceBus.Tests.Transport;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BusBootStrapperTests
    {
        private class FakeCommand : ICommand
        { }

        private class FakeEvent : IEvent
        { }

        private class FakeBootstrapperConfig : IBusBootstrapperConfiguration
        {
            public string DirectoryServiceCommandEndpoint { get { return "command"; } }
            public string DirectoryServiceEventEndpoint { get { return "event"; } }
            public string DirectoryServiceName { get { return "name"; } }
        }

        private BusBootstrapper _bootstrapper;
        private Mock<IMessageOptionsRepository> _repoMock;
        private Mock<IMessageSender> _senderMock;
        private Mock<ISubscriptionManager> _subscriptionManagerMock;
        private Mock<IAssemblyScanner> _assemblyScannerMock;
        private FakeBootstrapperConfig _config;
        private Mock<IPeerManager> _peerManagerMock;
        private FakeTransportConfiguration _configTransport;
        private Mock<ICompletionCallback> _completionCallbackMock;

        [SetUp]
        public void setup()
        {
            _peerManagerMock = new Mock<IPeerManager>();
            _configTransport = new FakeTransportConfiguration();
            _config = new FakeBootstrapperConfig();
            _assemblyScannerMock = new Mock<IAssemblyScanner>();
            _senderMock = new Mock<IMessageSender>();
            _subscriptionManagerMock = new Mock<ISubscriptionManager>();
            _repoMock = new Mock<IMessageOptionsRepository>();
            _completionCallbackMock = new Mock<ICompletionCallback>();
            _senderMock.Setup(x => x.Send(It.IsAny<ICommand>(), It.IsAny<ICompletionCallback>())).Returns(
                _completionCallbackMock.Object);
            _bootstrapper = new BusBootstrapper(_assemblyScannerMock.Object, _configTransport, _config, _repoMock.Object, _senderMock.Object, _peerManagerMock.Object, _subscriptionManagerMock.Object);
        }


        [Test]
        public void should_register_directory_service_infrastructure_messages()
        {
            _bootstrapper.BootStrapTopology();

            _repoMock.Verify(x => x.RegisterOptions(It.Is<MessageOptions>(y => y.MessageType == typeof(InitializeTopologyAndMessageSettings).FullName && y.ReliabilityInfo.ReliabilityLevel == ReliabilityLevel.FireAndForget)));
            _repoMock.Verify(x => x.RegisterOptions(It.Is<MessageOptions>(y => y.MessageType == typeof(RegisterPeerCommand).FullName && y.ReliabilityInfo.ReliabilityLevel == ReliabilityLevel.FireAndForget)));
        }

        [Test]
        public void should_register_completion_acks_reliability()
        {
            _bootstrapper.BootStrapTopology();

            _repoMock.Verify(x => x.RegisterOptions(It.Is<MessageOptions>(y => y.MessageType == typeof(CompletionAcknowledgementMessage).FullName && y.ReliabilityInfo.ReliabilityLevel == ReliabilityLevel.FireAndForget)));
        }


        [Test]
        public void should_register_events_with_subscription_manager()
        {
            _assemblyScannerMock.Setup(x => x.GetHandledEvents()).Returns(new List<Type> { typeof(FakeEvent) });

            _bootstrapper.BootStrapTopology();
            
            _subscriptionManagerMock.Verify(x => x.StartListeningTo(typeof(FakeEvent)));
            
        }

        [Test]
        public void should_register_with_directory_service_and_wait_for_completion()
        {
            RegisterPeerCommand command = null;

            _senderMock.Setup(x => x.Send(It.IsAny<ICommand>(), It.IsAny<ICompletionCallback>())).Returns(_completionCallbackMock.Object).Callback<ICommand, ICompletionCallback>((y, z) => command = (RegisterPeerCommand)y);
            _assemblyScannerMock.Setup(x => x.GetHandledCommands()).Returns(new List<Type> { typeof(FakeCommand) });
            _assemblyScannerMock.Setup(x => x.GetSentEvents()).Returns(new List<Type> { typeof(FakeEvent) });

            _bootstrapper.BootStrapTopology();

            Assert.AreEqual(_configTransport.PeerName, command.Peer.PeerName);
            Assert.AreEqual(_configTransport.GetEventsConnectEndpoint(), command.Peer.PublicationEndpoint);
            Assert.AreEqual(_configTransport.GetCommandsConnectEnpoint(), command.Peer.ReceptionEndpoint);
            Assert.AreEqual(typeof(FakeEvent), command.Peer.PublishedMessages.Single());
            Assert.AreEqual(typeof(FakeCommand), command.Peer.HandledMessages.Single());

            _completionCallbackMock.Verify(x => x.WaitForCompletion());
        }

        [Test]
        public void should_register_directory_service_as_peer()
        {
            IServicePeer dirServicePeer = null;
            _peerManagerMock.Setup(x => x.RegisterPeer(It.IsAny<IServicePeer>())).Callback<IServicePeer>(x => dirServicePeer = x);
            
            _bootstrapper.BootStrapTopology();

            Assert.AreEqual(_config.DirectoryServiceName, dirServicePeer.PeerName);
            Assert.AreEqual(_config.DirectoryServiceEventEndpoint, dirServicePeer.PublicationEndpoint);
            Assert.AreEqual(_config.DirectoryServiceCommandEndpoint, dirServicePeer.ReceptionEndpoint);
            Assert.AreEqual(typeof(PeerConnected), dirServicePeer.PublishedMessages.Single());
            Assert.AreEqual(typeof(RegisterPeerCommand), dirServicePeer.HandledMessages.Single());
        }
    }
}