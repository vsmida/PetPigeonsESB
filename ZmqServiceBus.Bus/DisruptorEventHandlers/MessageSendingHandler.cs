using System;
using System.Collections.Generic;
using System.Linq;
using Disruptor;
using ZmqServiceBus.Bus.InfrastructureMessages;
using ZmqServiceBus.Bus.MessageInterfaces;
using ZmqServiceBus.Bus.Transport;
using ZmqServiceBus.Bus.Transport.Network;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.DisruptorEventHandlers
{
    public class MessageSendingHandler : IEventHandler<OutboundMessageProcessingEntry>
    {
        private readonly ICallbackRepository _callbackRepository;
        private readonly IPeerManager _peerManager;
        private readonly IPeerConfiguration _peerConfiguration;

        private Dictionary<string, HashSet<string>> _peersToShadows;
        private Dictionary<string, List<MessageSubscription>> _messageTypesToSubscriptions;
        private IEnumerable<string> _selfShadows;


        public MessageSendingHandler(ICallbackRepository callbackRepository, IPeerManager peerManager, IPeerConfiguration peerConfiguration)
        {
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;
            _peerConfiguration = peerConfiguration;
            _peerManager.PeerConnected += OnPeerChange;
        }

        private void OnPeerChange(ServicePeer obj)
        {
            _peersToShadows = _peerManager.GetAllShadows();
            _messageTypesToSubscriptions = _peerManager.GetAllSubscriptions();
            _selfShadows = _peerManager.PeersThatShadowMe();
        }

        public void OnNext(OutboundMessageProcessingEntry data, long sequence, bool endOfBatch)
        {
            if (data.TargetPeer != null)
            {
                Route(data);
            }
            else
            {
                Send(data);
            }
        }

        private void Send(OutboundMessageProcessingEntry data)
        {
            var concernedSubscriptions =
                _messageTypesToSubscriptions[data.Message.GetType().FullName].Where(
                    x => x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(data.Message));
            SendUsingSubscriptions(data.Message, data.Callback, concernedSubscriptions, data);
        }

        private void Route(OutboundMessageProcessingEntry data)
        {

            var subscription = _peerManager.GetPeerSubscriptionFor(data.Message.GetType().FullName, data.TargetPeer);
            SendUsingSubscriptions(data.Message, data.Callback, new[] { subscription }, data);

        }
        
        private void SendUsingSubscriptions(IMessage message, ICompletionCallback callback, IEnumerable<MessageSubscription> concernedSubscriptions, OutboundMessageProcessingEntry disruptorData)
        {
            var serializedMessage = BusSerializer.Serialize(message);
            var messageId = Guid.NewGuid();
            var messageType = message.GetType().FullName;
            var messageData = new MessageWireData(messageType, messageId, serializedMessage);

            if (callback != null)
                _callbackRepository.RegisterCallback(messageData.MessageIdentity, callback);
            foreach (var concernedSubscription in concernedSubscriptions)
            {
                var wireMessage = new WireSendingMessage(messageData, concernedSubscription.Endpoint);
                disruptorData.WireMessages.Add(wireMessage);
            }

            SendShadowMessages(concernedSubscriptions, messageData, disruptorData);

            if (disruptorData.IsAcknowledgement)
            {
                var completionAcknowledgementMessage = (CompletionAcknowledgementMessage) message;
                SendToSelfShadows(messageId, completionAcknowledgementMessage.ProcessingSuccessful,disruptorData.TargetPeer, disruptorData);
            }

        }

        private void SendShadowMessages(IEnumerable<MessageSubscription> concernedSubscriptions, MessageWireData messageData, OutboundMessageProcessingEntry disruptorData)
        {
            foreach (var peer in concernedSubscriptions.Select(x => x.Peer).Distinct())
            {
                HashSet<string> targetShadows;
                if (_peersToShadows.TryGetValue(peer, out targetShadows))
                {
                    var shadowMessage = new ShadowMessageCommand(messageData, peer, true);
                    var shadowSubscriptions = _messageTypesToSubscriptions[typeof(ShadowMessageCommand).FullName];

                    var serializedShadowMessage = BusSerializer.Serialize(shadowMessage);
                    var shadowMessageId = Guid.NewGuid();
                    var shadowMessageData = new MessageWireData(typeof(ShadowMessageCommand).FullName,
                                                                shadowMessageId,
                                                                serializedShadowMessage);

                    foreach (var peerShadow in targetShadows)
                    {
                        var endpoint = shadowSubscriptions.Single(x => x.Peer == peerShadow).Endpoint;
                        var wireMessage = new WireSendingMessage(shadowMessageData, endpoint);
                        disruptorData.WireMessages.Add(wireMessage);
                    }
                }
            }
        }

        private void SendToSelfShadows(Guid messageId, bool processSuccessful, string originatingPeer, OutboundMessageProcessingEntry data)
        {
            var message = new ShadowCompletionMessage(messageId,
                                                      originatingPeer,
                                                      _peerConfiguration.PeerName,
                                                      processSuccessful);
            foreach (var selfShadow in _selfShadows ?? Enumerable.Empty<string>())
            {
                var subscription = _peerManager.GetPeerSubscriptionFor(data.Message.GetType().FullName, selfShadow);
                SendUsingSubscriptions(message, null, new[] { subscription }, data);
            }
        }
    }
}