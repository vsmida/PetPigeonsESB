using System.Collections.Generic;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Handlers
{
    public class PersistenceShadowMessagesHandler : ICommandHandler<ShadowMessageCommand>, ICommandHandler<ShadowCompletionMessage>
    {
        private readonly ISavedMessagesStore _messagesStore;

        public PersistenceShadowMessagesHandler(IPeerConfiguration peerConfiguration, ISavedMessagesStore messagesStore)
        {
            _messagesStore = messagesStore;
        }

        public void Handle(ShadowMessageCommand item)
        {
            _messagesStore.SaveMessage(item);
        }

        public void Handle(ShadowCompletionMessage item)
        {
            _messagesStore.RemoveMessage(item.FromPeer, item.TransportType, item.MessageId);
        }
    }
}