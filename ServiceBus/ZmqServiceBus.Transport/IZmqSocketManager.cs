using System.Collections.Concurrent;
using Shared;
using ZmqServiceBus.Contracts;

namespace ZmqServiceBus.Transport
{
    public interface IZmqSocketManager
    {
        void CreateSubscribeSocket(BlockingCollection<ITransportMessage> receiveQueue);
        void CreateRequestSocket(BlockingCollection<ITransportMessage> sendingQueue, BlockingCollection<ITransportMessage> acknowledgementQueue, string endpoint, string servicePeerName);
        void CreatePublisherSocket(BlockingCollection<ITransportMessage> sendingQueue, string endpoint, string servicePeerName);
        void CreateResponseSocket(BlockingCollection<ITransportMessage> receivingQueue, string endpoint, string servicePeerName);
        void Stop();
        void SubscribeTo(string endpoint, string messageType);
    }
}