using Shared;
using ZmqServiceBus.Bus;
using ZmqServiceBus.Bus.Transport;

namespace ZmqServiceBus.Tests.Transport
{
    public class FakeTransportConfiguration : ZmqTransportConfiguration
    {
        private int? _eventsPort;
        private int? _commandsPort;

        public override int EventsPort
        {
            get
            {
                if (_eventsPort == null)
                    _eventsPort = NetworkUtils.GetRandomUnusedPort();
                return _eventsPort.Value;
            }
         //    set { _eventsPort = value; }
        }

        public override int CommandsPort
        {
            get { if(_commandsPort == null)
            {
                _commandsPort = NetworkUtils.GetRandomUnusedPort();
            }
                return _commandsPort.Value;
            }
      //      set { _commandsPort = value; }
        }

        public override string EventsProtocol { get { return "tcp"; }  }
        public override string CommandsProtocol { get { return "tcp"; } }

        public override string PeerName
        {
            get { return "FakePeerName"; }
        }
    }
}