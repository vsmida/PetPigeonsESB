using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Disruptor;

namespace Bus.Transport.SendingPipe
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