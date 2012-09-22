using Shared;
using ZmqServiceBus.Transport;

namespace ZmqServiceBus.Tests.Transport
{
    public class FakeTransportConfiguration : ITransportConfiguration
    {
        private int? _eventsPort;
        private int? _commandsPort;

        public int EventsPort
        {
            get
            {
                if (_eventsPort == null)
                    _eventsPort = NetworkUtils.GetRandomUnusedPort();
                return _eventsPort.Value;
            }
             set { _eventsPort = value; }
        }

        public int CommandsPort
        {
            get { if(_commandsPort == null)
            {
                _commandsPort = NetworkUtils.GetRandomUnusedPort();
            }
                return _commandsPort.Value;
            }
            set { _commandsPort = value; }
        }

        public string EventsProtocol { get { return "inproc"; }  }
        public string CommandsProtocol { get { return "inproc"; } }

        public string Identity { get { return "Identity"; } }
    }
}