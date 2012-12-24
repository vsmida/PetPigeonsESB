namespace ZmqServiceBus.Bus.Transport.Network
{
    public enum WireSendingTransportType
    {
        ZmqPushTransport
    }

    public interface IEndpoint
    {
        WireSendingTransportType WireTransportType { get; }
    }
}