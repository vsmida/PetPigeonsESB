using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    public interface ISendingReliabilityStrategy
    {
        void SendOn(IEndpointManager endpointManager, ISendingTransportMessage message);
        void PublishOn(IEndpointManager endpointManager, ISendingTransportMessage message);
        void RouteOn(IEndpointManager endpointManager, ISendingTransportMessage message, string destinationPeer);
    }
}