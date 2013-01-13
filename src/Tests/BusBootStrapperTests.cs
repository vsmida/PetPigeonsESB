using Moq;
using NUnit.Framework;
using Shared;
using Bus;
using Bus.Dispatch;
using Bus.MessageInterfaces;
using Bus.Startup;
using Bus.Subscriptions;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using Tests.Transport;

namespace Tests
{
    [TestFixture]
    public class BusBootStrapperTests
    {
        private class FakeCommand : ICommand
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }
            
        }

        private class FakeEvent : IEvent
        {
            public ReliabilityLevel DesiredReliability { get { return ReliabilityLevel.FireAndForget; } }
            
        }

        private class FakeBootstrapperConfig : IBusBootstrapperConfiguration
        {
            public string DirectoryServiceEndpoint { get { return "command"; } }
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
        private Mock<IPeerConfiguration> _peerConfigurationMock;
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
            _peerConfigurationMock = new Mock<IPeerConfiguration>();
            _bootstrapper = new BusBootstrapper(_assemblyScannerMock.Object, _configTransport, _config, _repoMock.Object, _senderMock.Object, _peerManagerMock.Object,
                _subscriptionManagerMock.Object, _peerConfigurationMock.Object);
        }
    }
}