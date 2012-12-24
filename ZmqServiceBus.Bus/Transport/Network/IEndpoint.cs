using ProtoBuf;

namespace ZmqServiceBus.Bus.Transport.Network
{
    [ProtoContract]
    public enum WireSendingTransportType
    {
        ZmqPushTransport
    }
    [ProtoInclude(1, typeof(ZmqEndpoint))]
    public interface IEndpoint
    {
        WireSendingTransportType WireTransportType { get; }
    }
}