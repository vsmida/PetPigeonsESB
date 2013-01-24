using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.SendingPipe;

namespace Bus.DisruptorEventHandlers
{
    public interface IReliabilityCoordinator
    {
        void EnsureReliability(OutboundDisruptorEntry disruptorEntry, IMessage message, MessageSubscription[] concernedSubscriptions, MessageWireData messageData);
    }
}