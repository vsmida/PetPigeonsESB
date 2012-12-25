using System;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.InfrastructureMessages
{
    public class HeartbeatRequest : ICommand
    {
        public readonly DateTime RequestTimeStamp;

        public HeartbeatRequest(DateTime requestTimeStamp)
        {
            RequestTimeStamp = requestTimeStamp;
        }
    }
}