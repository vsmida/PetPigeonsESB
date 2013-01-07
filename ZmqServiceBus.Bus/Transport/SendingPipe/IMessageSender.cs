using System;
using Disruptor;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;

namespace ZmqServiceBus.Bus.Transport.SendingPipe
{
    public interface IMessageSender
    {
        ICompletionCallback Send(ICommand command, ICompletionCallback callback = null);
        void Publish(IEvent message);
        ICompletionCallback Route(IMessage message, string peerName);
        void Acknowledge(Guid messageId, bool processSuccessful, string originatingPeer);
        void Initialize(RingBuffer<OutboundMessageProcessingEntry> buffer );
    }
}