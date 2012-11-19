using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : ISendingReliabilityStrategy
    {
        public void SendOn(IEndpointManager endpointManager, ISendingTransportMessage message)
        {
            endpointManager.SendMessage(message);
        }

        public void PublishOn(IEndpointManager endpointManager, ISendingTransportMessage message)
        {
            endpointManager.PublishMessage(message);
        }

        public void RouteOn(IEndpointManager endpointManager, ISendingTransportMessage message, string destinationPeer)
        {
            endpointManager.RouteMessage(message, destinationPeer);
        }


    }
}