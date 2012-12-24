namespace ZmqServiceBus.Bus.Transport.Network
{
    public class ZmqEndpoint : IEndpoint
    {
        public string Endpoint { get; set; }

        public ZmqEndpoint(string endpoint)
        {
            Endpoint = endpoint;
        }

        public WireSendingTransportType WireTransportType { get{return WireSendingTransportType.ZmqPushTransport;}}
    }
}