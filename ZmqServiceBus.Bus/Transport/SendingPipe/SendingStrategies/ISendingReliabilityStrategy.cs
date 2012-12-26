using System;
using System.Collections.Generic;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        void Send(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        void Publish(ISendingBusMessage message, IEnumerable<IMessageSubscription> concernedSubscriptions);
        event Action ReliabilityAchieved;
    }
}