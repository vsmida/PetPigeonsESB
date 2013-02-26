using System.Collections.Generic;
using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.SendingPipe;

namespace Bus.DisruptorEventHandlers
{
    interface IReliabilityCoordinator
    {
        void EnsureReliability(OutboundDisruptorEntry disruptorEntry, IMessage message, IEnumerable<MessageSubscription> concernedSubscriptions, MessageWireData messageData);
    }
}