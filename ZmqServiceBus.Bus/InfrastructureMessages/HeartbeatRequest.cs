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
    public class HeartbeatRequest : ICommand
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

        public ReliabilityLevel DesiredReliability
        {
            get { return ReliabilityLevel.FireAndForget; }
        }
    }
}