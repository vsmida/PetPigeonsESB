using System.Collections.Concurrent;

namespace ZmqServiceBus.Transport
{
    public interface IZmqSocketManager
    {
        void CreateSubscribeSocket(BlockingCollection<ITransportMessage> receiveQueue, string endpoint);
        void CreateRequestSocket(BlockingCollection<ITransportMessage> sendingQueue, BlockingCollection<ITransportMessage> acknowledgementQueue, string endpoint, string senderIdentity);
        void CreatePublisherSocket(BlockingCollection<ITransportMessage> sendingQueue, string endpoint);
        void CreateResponseSocket(BlockingCollection<ITransportMessage> receivingQueue, BlockingCollection<ITransportMessage> sendingQueue, string endpoint, string identity);
        void Stop();
    }
}