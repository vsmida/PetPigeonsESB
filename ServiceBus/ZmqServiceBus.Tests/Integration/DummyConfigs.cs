using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Tests.Integration
{
    public class DummyBootstrapperConfig : IBusBootstrapperConfiguration
    {
        public string DirectoryServiceEndpoint { get; set; }
        public string DirectoryServiceName { get; set; }
    }

    public class DummyTransportConfig : ZmqTransportConfiguration
    {
        private readonly int _port;
        private readonly string _peerName;

        public DummyTransportConfig(int port, string peerName)
        {
            _port = port;
            _peerName = peerName;
        }

        public override int Port
        {
            get { return _port; }
        }
        public override string Protocol
        {
            get { return "tcp"; }
        }

        public override string PeerName
        {
            get { return _peerName; }
        }
    }
}