using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Handlers
{
    public class PersistenceShadowMessagesHandler : ICommandHandler<ShadowMessageCommand>, ICommandHandler<ShadowCompletionMessage>, ICommandHandler<PublishUnacknowledgedMessagesToPeer>
    {
        private readonly ISavedMessagesStore _messagesStore;
        private readonly IMessageSender _messageSender;
        public PersistenceShadowMessagesHandler(ISavedMessagesStore messagesStore, IMessageSender messageSender)
        {
            _messagesStore = messagesStore;
            _messageSender = messageSender;
        }

        public void Handle(ShadowMessageCommand item)
        {
            _messagesStore.SaveMessage(item);
        }

        public void Handle(ShadowCompletionMessage item)
        {
            _messagesStore.RemoveMessage(item.FromPeer, item.TransportType, item.MessageId);
        }

        public void Handle(PublishUnacknowledgedMessagesToPeer item)
        {
            foreach (var wireTransportType in item.TransportType)
            {
                var messages = _messagesStore.GetFirstMessages(item.Peer, wireTransportType, 1000);
                foreach (var shadowMessageCommand in messages)
                {
                    var receivedTransportMessage = new ReceivedTransportMessage(shadowMessageCommand.Message.MessageType, shadowMessageCommand.Message.SendingPeer,
                                                                                shadowMessageCommand.Message.MessageIdentity, wireTransportType, shadowMessageCommand.Message.Data);
                    _messageSender.Route(new ProcessMessageCommand(receivedTransportMessage), item.Peer);
                }

            }
        }
    }
}