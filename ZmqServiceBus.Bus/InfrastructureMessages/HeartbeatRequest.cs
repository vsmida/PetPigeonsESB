using System;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class HeartbeatRequest : ICommand
    {
        public readonly DateTime RequestTimeStamp;
        public readonly IEndpoint Endpoint;

        public HeartbeatRequest(DateTime requestTimeStamp, IEndpoint endpoint)
        {
            RequestTimeStamp = requestTimeStamp;
            Endpoint = endpoint;
        }
    }
}