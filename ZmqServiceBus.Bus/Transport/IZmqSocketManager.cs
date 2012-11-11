using System.Collections.Concurrent;

namespace ZmqServiceBus.Bus.Transport
{
    public interface IZmqSocketManager
    {
        void CreateSubscribeSocket(BlockingCollection<IReceivedTransportMessage> receiveQueue);
        void CreateRequestSocket(BlockingCollection<IReceivedTransportMessage> sendingQueue, BlockingCollection<IReceivedTransportMessage> acknowledgementQueue, string endpoint, string servicePeerName);
        void CreatePublisherSocket(BlockingCollection<IReceivedTransportMessage> sendingQueue, string endpoint, string servicePeerName);
        void CreateResponseSocket(BlockingCollection<IReceivedTransportMessage> receivingQueue, string endpoint, string servicePeerName);
        void Stop();
        void SubscribeTo(string endpoint, string messageType);
    }
}