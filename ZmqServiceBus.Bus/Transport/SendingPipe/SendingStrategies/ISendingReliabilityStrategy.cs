using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        void SetupCommandReliabilitySafeguards(ISendingBusMessage message);
        void SetupEventReliabilitySafeguards(ISendingBusMessage message);
        event Action ReliabilityAchieved;
    }

    abstract class SendingReliabilityStrategy : ISendingReliabilityStrategy
    {
        public abstract void SetupCommandReliabilitySafeguards(ISendingBusMessage message);
        public abstract void SetupEventReliabilitySafeguards(ISendingBusMessage message);
        public abstract event Action ReliabilityAchieved;
    }
}