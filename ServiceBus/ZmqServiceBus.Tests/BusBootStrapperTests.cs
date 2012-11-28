using Moq;
using NUnit.Framework;
using Shared;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Tests
{
    [TestFixture]
    public class BusBootStrapperTests
    {
        private BusBootstrapper _bootstrapper;
        private Mock<IMessageOptionsRepository> _repoMock;

        [SetUp]
        public void setup()
        {
            _repoMock = new Mock<IMessageOptionsRepository>();
            _bootstrapper = new BusBootstrapper();
        }


        [Test]
        public void should_register_directory_service_infrastructure_messages()
        {
            _bootstrapper.BootStrapTopology();

            _repoMock.Verify(x => x.RegisterOptions(It.Is<MessageOptions>(y => y.MessageType == typeof(InitializeTopologyAndMessageSettings).FullName && y.ReliabilityInfo.ReliabilityLevel == ReliabilityLevel.FireAndForget)));
            _repoMock.Verify(x => x.RegisterOptions(It.Is<MessageOptions>(y => y.MessageType == typeof(RegisterPeerCommand).FullName && y.ReliabilityInfo.ReliabilityLevel == ReliabilityLevel.FireAndForget)));
        }
    }
}