namespace ZmqServiceBus.Transport
{
    public interface ITransportConfiguration
    {
        int EventsPort { get; }
        int CommandsPort { get; }
        string EventsProtocol { get; }
        string CommandsProtocol { get; }
        string Identity { get; }
    }
}