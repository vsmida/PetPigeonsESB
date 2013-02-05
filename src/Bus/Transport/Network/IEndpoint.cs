using System;
using ProtoBuf;

namespace Bus.Transport.Network
{
    [ProtoContract]
    public enum WireTransportType
    {
        ZmqPushPullTransport
    }
    [ProtoInclude(1, typeof(ZmqEndpoint))]
    public interface IEndpoint : IEquatable<IEndpoint>
    {
        WireTransportType WireTransportType { get; }
        bool IsMulticast { get; }
    }
}