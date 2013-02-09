using System;
using ProtoBuf;

namespace Bus.Transport.Network
{
    [ProtoContract]
    public enum WireTransportType
    {
        ZmqPushPullTransport = 0
    }
    [ProtoInclude(1, typeof(ZmqEndpoint))]
    public interface IEndpoint : IEquatable<IEndpoint>
    {
        WireTransportType WireTransportType { get; }
        bool IsMulticast { get; }
    }
}