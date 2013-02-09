using System;
using Bus.Transport.Network;
using ProtoBuf;
using Shared;

namespace Bus
{
    [ProtoContract]
    public class MessageOptions
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly Type MessageType;
        [ProtoMember(2, IsRequired = true)]
        public readonly ReliabilityLevel ReliabilityLevel;
        [ProtoMember(3, IsRequired = true)]
        public readonly WireTransportType TransportType;

        public MessageOptions(Type messageType, ReliabilityLevel reliabilityLevel, WireTransportType transportType)
        {
            MessageType = messageType;
            ReliabilityLevel = reliabilityLevel;
            TransportType = transportType;
        }

        private MessageOptions()
        {
        }
    }


}