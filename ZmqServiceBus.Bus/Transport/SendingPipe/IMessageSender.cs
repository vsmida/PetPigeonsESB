using System;
using Disruptor;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface IMessageSender : IDisposable
    {
        ICompletionCallback Send(ICommand command, ICompletionCallback callback = null);
        void Publish(IEvent message);
        ICompletionCallback Route(IMessage message, string peerName);
        void Acknowledge(Guid messageId,string messageType, bool processSuccessful, string originatingPeer, WireTransportType transportType);
        void SendHeartbeat(IEndpoint endpoint);
        void Initialize(RingBuffer<OutboundDisruptorEntry> buffer );

    }
}