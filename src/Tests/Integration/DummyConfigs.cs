using Bus.Startup;
using Bus.Transport;

namespace Tests.Integration
{
    public class DummyBootstrapperConfig : IBusBootstrapperConfiguration
    {
        public string DirectoryServiceEndpoint { get; set; }
        public string DirectoryServiceName { get; set; }
    }

    public class DummyTransportConfig : ZmqTransportConfiguration
    {
        private readonly int _port;

        public DummyTransportConfig(int port)
        {
            _port = port;
        }

        public override int Port
        {
            get { return _port; }
        }
        public override string Protocol
        {
            get { return "tcp"; }
        }
    }
}