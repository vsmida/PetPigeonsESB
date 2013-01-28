using System.Collections.Generic;
using System.Diagnostics;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Shadowing;
using Bus.MessageInterfaces;
using Bus.Transport.ReceptionPipe;
using Bus.Transport.SendingPipe;
using log4net;
using System.Linq;

namespace Bus.Handlers
{
    class PersistenceShadowMessagesHandler : ICommandHandler<ShadowMessageCommand>,
                                                    ICommandHandler<ShadowCompletionMessage>,
                                                    ICommandHandler<PublishUnacknowledgedMessagesToPeer>,
                                                    ICommandHandler<SynchronizeWithBrokerCommand>,
                                                    ICommandHandler<StopSynchWithBrokerCommand>
    {
        private readonly ISavedMessagesStore _messagesStore;
        private readonly IMessageSender _messageSender;
        private readonly ILog _logger = LogManager.GetLogger(typeof (PersistenceShadowMessagesHandler));
        private static readonly Dictionary<string, bool> _peersInitializing = new Dictionary<string, bool>();
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
            bool isInitializing;
            _peersInitializing.TryGetValue(item.PrimaryRecipient, out isInitializing);
            if(isInitializing)
            {
                var receivedTransportMessage = new ReceivedTransportMessage(item.Message.MessageType, item.Message.SendingPeer,
                                                            item.Message.MessageIdentity, item.TargetEndpoint,
                                                            item.Message.Data, item.Message.SequenceNumber);
                _messageSender.Route(new ProcessMessageCommand(receivedTransportMessage), item.PrimaryRecipient);
            }

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

        public void Handle(SynchronizeWithBrokerCommand item)
        {
            _peersInitializing[item.PeerName] = true;
            var messages = _messagesStore.GetFirstMessages(item.PeerName, null);
            _logger.DebugFormat("Synchronizing with peer {0}, message count = {1}", item.PeerName, messages.Count());
            foreach (var shadowMessageCommand in messages)
            {
                var receivedTransportMessage = new ReceivedTransportMessage(shadowMessageCommand.Message.MessageType, shadowMessageCommand.Message.SendingPeer,
                                                                            shadowMessageCommand.Message.MessageIdentity, shadowMessageCommand.TargetEndpoint,
                                                                            shadowMessageCommand.Message.Data, shadowMessageCommand.Message.SequenceNumber);
                _messageSender.Route(new ProcessMessageCommand(receivedTransportMessage), item.PeerName);
            }

        }

        public void Handle(StopSynchWithBrokerCommand item)
        {
            _logger.DebugFormat("Stop synching with peer {0}", item.PeerName);
            _peersInitializing[item.PeerName] = false;
            _messageSender.Route(new EndOfPersistedMessages(), item.PeerName);

        }
    }
}