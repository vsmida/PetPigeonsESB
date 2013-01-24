using System.Diagnostics;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Shadowing;
using Bus.MessageInterfaces;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;

namespace Bus.Handlers
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
            if (item == null)
                Debugger.Break();
            _messagesStore.SaveMessage(item);
        }

        public void Handle(ShadowCompletionMessage item)
        {
            _messagesStore.RemoveMessage(item);
        }


        public void Handle(PublishUnacknowledgedMessagesToPeer item)
        {
            var messages = _messagesStore.GetFirstMessages(item.Peer, null);
            foreach (var shadowMessageCommand in messages)
            {
                var receivedTransportMessage = new ReceivedTransportMessage(shadowMessageCommand.Message.MessageType, shadowMessageCommand.Message.SendingPeer,
                                                                            shadowMessageCommand.Message.MessageIdentity, shadowMessageCommand.TargetEndpoint,
                                                                            shadowMessageCommand.Message.Data, -1);
                _messageSender.Route(new ProcessMessageCommand(receivedTransportMessage), item.Peer);
            }

            _messageSender.Route(new EndOfPersistedMessages(), item.Peer);
        }

    }
}