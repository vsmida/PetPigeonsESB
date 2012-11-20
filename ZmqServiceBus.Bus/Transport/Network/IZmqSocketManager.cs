using System.Collections.Concurrent;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IZmqSocketManager
    {
        void CreateSubscribeSocket(BlockingCollection<IReceivedTransportMessage> receiveQueue);
        void CreateRequestSocket(BlockingCollection<ISendingTransportMessage> sendingQueue, BlockingCollection<IReceivedTransportMessage> acknowledgementQueue, string endpoint, string servicePeerName);
        void CreatePublisherSocket(BlockingCollection<ISendingTransportMessage> sendingQueue, string endpoint, string servicePeerName);
        void CreateResponseSocket(BlockingCollection<IReceivedTransportMessage> receivingQueue, string endpoint, string servicePeerName);
        void Stop();
        void SubscribeTo(string endpoint, string messageType);
    }
}