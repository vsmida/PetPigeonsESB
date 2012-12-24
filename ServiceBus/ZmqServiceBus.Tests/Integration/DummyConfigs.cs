using ZmqServiceBus.Bus.Startup;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Tests.Integration
{
    public class DummyBootstrapperConfig : IBusBootstrapperConfiguration
    {
        public string DirectoryServiceCommandEndpoint { get; set; }
        public string DirectoryServiceEventEndpoint { get; set; }
        public string DirectoryServiceName { get; set; }
    }

    public class DummyTransportConfig : ZmqTransportConfiguration
    {
        private readonly int _eventPort;
        private readonly int _commandPort;
        private readonly string _peerName;

        public DummyTransportConfig(int eventPort, int commandPort, string peerName)
        {
            _eventPort = eventPort;
            _commandPort = commandPort;
            _peerName = peerName;
        }

        public override int EventsPort
        {
            get { return _eventPort; }
        }

        public override int CommandsPort
        {
            get { return _commandPort; }
        }

        public override string EventsProtocol
        {
            get { return "tcp"; }
        }

        public override string CommandsProtocol
        {
            get { return "tcp"; }
        }

        public override string PeerName
        {
            get { return _peerName; }
        }
    }
}