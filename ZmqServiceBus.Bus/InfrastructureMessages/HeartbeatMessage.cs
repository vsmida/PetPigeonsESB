using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class HeartbeatMessage : IMessage
    {
        public readonly DateTime TimestampUtc;

        public HeartbeatMessage(DateTime timestampUtc)
        {
            TimestampUtc = timestampUtc;
        }
    }
}