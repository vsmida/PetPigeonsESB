using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using ProtoBuf;
using Shared;
using Shared.Attributes;

namespace Bus.InfrastructureMessages.Heartbeating
{
    [InfrastructureMessage]
    [ProtoContract]
    class HeartbeatRequest : ICommand
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly DateTime RequestTimeStamp;
        [ProtoMember(2, IsRequired = true)]
        public readonly IEndpoint Endpoint;

        public HeartbeatRequest(DateTime requestTimeStamp, IEndpoint endpoint)
        {
            RequestTimeStamp = requestTimeStamp;
            Endpoint = endpoint;
        }
    }
}