using System;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        void SetupReliabilitySafeguards(SendingBusMessage message);
        void RegisterAck(Guid messageId, string originatingPeer);
        event Action ReliabilityAchieved;
    }

    abstract class SendingReliabilityStrategy : ISendingReliabilityStrategy
    {
        public abstract void SetupReliabilitySafeguards(SendingBusMessage message);
        public abstract void RegisterAck(Guid messageId, string originatingPeer);
        public abstract event Action ReliabilityAchieved;
    }
}