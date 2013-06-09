using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;
using Disruptor;

namespace Bus.Transport.SendingPipe
{
    interface IMessageSender : IDisposable
    {
        ICompletionCallback Send(ICommand command, ICompletionCallback callback = null);
        void Publish(IEvent message);
        ICompletionCallback Route(IMessage message, PeerId peerName);
        void Acknowledge(Guid messageId, string messageType, bool processSuccessful, PeerId originatingPeer, IEndpoint endpoint);
        void SendHeartbeat(IEndpoint endpoint);
        void Initialize(RingBuffer<OutboundDisruptorEntry> buffer );
        void InjectNetworkSenderCommand(IBusEventProcessorCommand command);

    }
}