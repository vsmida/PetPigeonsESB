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
    public class MessageTargetsHandler : IEventHandler<OutboundDisruptorEntry>
    {
        private readonly ICallbackRepository _callbackRepository;
        private readonly IPeerManager _peerManager;
        private readonly IPeerConfiguration _peerConfiguration;

        private Dictionary<string, HashSet<string>> _peersToShadows;
        private Dictionary<string, List<MessageSubscription>> _messageTypesToSubscriptions;
        private IEnumerable<string> _selfShadows;

        public MessageTargetsHandler(ICallbackRepository callbackRepository, IPeerManager peerManager, IPeerConfiguration peerConfiguration)
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

        public void OnNext(OutboundDisruptorEntry data, long sequence, bool endOfBatch)
        {
            if (data.MessageTargetHandlerData.Message == null)
                return;

            var messageType = data.MessageTargetHandlerData.Message.GetType().FullName;
            var subscriptions = _messageTypesToSubscriptions[messageType]
                                .Where(x => (x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(data.MessageTargetHandlerData.Message))
                                             && (data.MessageTargetHandlerData.TargetPeer == null || x.Peer == data.MessageTargetHandlerData.TargetPeer)).ToArray();

            SendUsingSubscriptions(data.MessageTargetHandlerData.Message, data.MessageTargetHandlerData.Callback, subscriptions, data);

        }

        private void SendUsingSubscriptions(IMessage message, ICompletionCallback callback, MessageSubscription[] concernedSubscriptions, OutboundDisruptorEntry disruptorData)
        {
            var messageData = CreateMessageWireData(message);

            if (callback != null)
                _callbackRepository.RegisterCallback(messageData.MessageIdentity, callback);
            
            foreach (var concernedSubscription in concernedSubscriptions)
            {
                var wireMessage = new WireSendingMessage(messageData, concernedSubscription.Endpoint);
                disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
            }

            SendShadowMessages(concernedSubscriptions, messageData, disruptorData);

            if (disruptorData.MessageTargetHandlerData.IsAcknowledgement)
            {
                var completionAcknowledgementMessage = (CompletionAcknowledgementMessage)message;
                SendToSelfShadows(messageData.MessageIdentity, completionAcknowledgementMessage.ProcessingSuccessful, disruptorData.MessageTargetHandlerData.TargetPeer,completionAcknowledgementMessage.TransportType, disruptorData);
            }

        }

        private MessageWireData CreateMessageWireData(IMessage message)
        {
            var serializedMessage = BusSerializer.Serialize(message);
            var messageId = Guid.NewGuid();
            var messageType = message.GetType().FullName;
            var messageData = new MessageWireData(messageType, messageId,_peerConfiguration.PeerName ,serializedMessage);
            return messageData;
        }

        private void SendShadowMessages(IEnumerable<MessageSubscription> concernedSubscriptions, MessageWireData messageData, OutboundDisruptorEntry disruptorData)
        {
            foreach (var peer in concernedSubscriptions.Select(x => x.Peer).Distinct())
            {
                HashSet<string> targetShadows;
                if (_peersToShadows.TryGetValue(peer, out targetShadows))
                {
                    var shadowSubscriptions = _messageTypesToSubscriptions[typeof(ShadowMessageCommand).FullName];
                    var shadowMessage = new ShadowMessageCommand(messageData, peer, true);
                    var shadowMessageData = CreateMessageWireData(shadowMessage);

                    foreach (var peerShadow in targetShadows)
                    {
                        var endpoint = shadowSubscriptions.Single(x => x.Peer == peerShadow).Endpoint;
                        var wireMessage = new WireSendingMessage(shadowMessageData, endpoint);
                        disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
                    }
                }
            }
        }

        private void SendToSelfShadows(Guid messageId, bool processSuccessful, string originatingPeer,WireTransportType transportType, OutboundDisruptorEntry data)
        {
            var message = new ShadowCompletionMessage(messageId,
                                                      originatingPeer,
                                                      _peerConfiguration.PeerName,
                                                      processSuccessful, transportType);
            foreach (var selfShadow in _selfShadows ?? Enumerable.Empty<string>())
            {
                var messageType = data.MessageTargetHandlerData.Message.GetType().FullName;
                var subscription = _messageTypesToSubscriptions[messageType].Where(x => x.Peer == selfShadow).ToArray();
                SendUsingSubscriptions(message, null, subscription, data);
            }
        }
    }
}