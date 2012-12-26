using System;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class HeartbeatMessage : IMessage
    {
        public readonly DateTime TimestampUtc;
        public readonly IEndpoint Endpoint;

        public HeartbeatMessage(DateTime timestampUtc, IEndpoint endpoint)
        {
            TimestampUtc = timestampUtc;
            Endpoint = endpoint;
        }
    }
}