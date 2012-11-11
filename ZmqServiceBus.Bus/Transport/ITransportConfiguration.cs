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

    public abstract class TransportConfiguration : ITransportConfiguration
    {
        public string GetCommandsEnpoint()
        {
            return CommandsProtocol + "://*:" + CommandsPort;
        }

        public string GetEventsEndpoint()
        {
            return EventsProtocol + "://*:" + EventsPort;
        }

        public abstract int EventsPort { get; }
        public abstract int CommandsPort { get; }
        public abstract string EventsProtocol { get; }
        public abstract string CommandsProtocol { get; }
        public abstract string PeerName { get;}
    }
}