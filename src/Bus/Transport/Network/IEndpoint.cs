using System;
using System.IO;
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


    internal interface IEndpointSerializer
    {
        Stream Serialize(IEndpoint endpoint);
        IEndpoint Deserialize(Stream stream);
    }

    public abstract class EndpointSerializer<T> : IEndpointSerializer where T: IEndpoint
    {
        public Stream Serialize(IEndpoint item)
        {
            return Serialize((T)item);
        }

        IEndpoint IEndpointSerializer.Deserialize(Stream serializedMessage)
        {
            return Deserialize(serializedMessage);
        }

        public abstract Stream Serialize(T item);
        public abstract T Deserialize(Stream item);
    }


}