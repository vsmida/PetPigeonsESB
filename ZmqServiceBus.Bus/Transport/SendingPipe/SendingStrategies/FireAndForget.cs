using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport.SendingPipe.SendingStrategies
{
    internal class FireAndForget : ISendingReliabilityStrategy
    {
        public void SendOn(IEndpointManager endpointManager, ISendingBusMessage message)
        {
            endpointManager.SendMessage(message);
        }

        public void PublishOn(IEndpointManager endpointManager, ISendingBusMessage message)
        {
            endpointManager.PublishMessage(message);
        }

        public void RouteOn(IEndpointManager endpointManager, ISendingBusMessage message, string destinationPeer)
        {
            endpointManager.RouteMessage(message, destinationPeer);
        }


    }
}