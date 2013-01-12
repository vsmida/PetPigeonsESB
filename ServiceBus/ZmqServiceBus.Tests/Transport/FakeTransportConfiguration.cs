using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Tests.Transport
{
    public class FakeTransportConfiguration : ZmqTransportConfiguration
    {
        private int? _port;

        public override int Port
        {
            get
            {
                if (_port == null)
                    _port = NetworkUtils.GetRandomUnusedPort();
                return _port.Value;
            }
         //    set { _port = value; }
        }

   
        public override string Protocol { get { return "tcp"; } }
    }
}