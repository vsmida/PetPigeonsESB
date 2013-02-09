using System;
using Bus.Attributes;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;

namespace Bus.InfrastructureMessages.Heartbeating
{
    [InfrastructureMessage]
    [ProtoContract]
    public class HeartbeatMessage : IMessage, ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly DateTime TimestampUtc;
        [ProtoMember(2, IsRequired = true)]
        public readonly IEndpoint Endpoint;

        public HeartbeatMessage(DateTime timestampUtc, IEndpoint endpoint)
        {
            TimestampUtc = timestampUtc;
            Endpoint = endpoint;
        }


    }
}