using System.Configuration;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public interface ITransportConfiguration
    {
        int EventsPort { get; }
        int CommandsPort { get; }
        string EventsProtocol { get; }
        string CommandsProtocol { get; }
        string PeerName { get; }


    }

    public class TransportConfigurationRandomPort : TransportConfiguration
    {
        private readonly int _eventsPort;
        private readonly int _commandsPort;

        public TransportConfigurationRandomPort()
        {
            _eventsPort = NetworkUtils.GetRandomUnusedPort();
            _commandsPort = NetworkUtils.GetRandomUnusedPort();
        }

        public override int EventsPort
        {
            get { return _eventsPort; }
        }

        public override int CommandsPort
        {
            get { return _commandsPort; }
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
            get { return ConfigurationManager.AppSettings["ServiceName"]; }
        }
    }

    public abstract class TransportConfiguration : ITransportConfiguration
    {
        public string GetCommandsBindEnpoint()
        {
            return CommandsProtocol + "://*:" + CommandsPort;
        }

        public string GetEventsBindEndpoint()
        {
            return EventsProtocol + "://*:" + EventsPort;
        }

        public string GetCommandsConnectEnpoint()
        {
            return CommandsProtocol + "://" + NetworkUtils.GetOwnIp() + ":" + CommandsPort;
        }

        public string GetEventsConnectEndpoint()
        {
            return EventsProtocol + "://" + NetworkUtils.GetOwnIp() + ":" + EventsPort;
        }

        public abstract int EventsPort { get; }
        public abstract int CommandsPort { get; }
        public abstract string EventsProtocol { get; }
        public abstract string CommandsProtocol { get; }
        public abstract string PeerName { get; }

    }
}