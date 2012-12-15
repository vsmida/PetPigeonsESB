using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        void SendOn(IEndpointManager endpointManager, ISendingBusMessage message);
        void PublishOn(IEndpointManager endpointManager, ISendingBusMessage message);
        void RouteOn(IEndpointManager endpointManager, ISendingBusMessage message, string destinationPeer);
    }
}