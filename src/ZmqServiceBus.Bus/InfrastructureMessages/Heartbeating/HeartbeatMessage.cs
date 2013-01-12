using System;
using ProtoBuf;
using Shared;
using Shared.Attributes;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.InfrastructureMessages
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