using System;
using System.Collections.Generic;
using System.Linq;
using Disruptor;
using Shared;
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
        private readonly IMessageOptionsRepository _optionsRepository;
        private Dictionary<string, MessageOptions> _messageOptions;

        public MessageTargetsHandler(ICallbackRepository callbackRepository, IPeerManager peerManager, IPeerConfiguration peerConfiguration, IMessageOptionsRepository optionsRepository)
        {
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;
            _peerConfiguration = peerConfiguration;
            _optionsRepository = optionsRepository;
            _peerManager.PeerConnected += OnPeerChange;
            _optionsRepository.OptionsUpdated += OnOptionsUpdated;
        }

        private void OnOptionsUpdated(MessageOptions obj)
        {
            _messageOptions = _optionsRepository.GetAllOptions();
        }

        private void OnPeerChange(ServicePeer obj)
        {
            //reference assignement is atomic;
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

            if (disruptorData.MessageTargetHandlerData.IsAcknowledgement)
            {
                var completionAcknowledgementMessage = (CompletionAcknowledgementMessage)message;
                if (_messageOptions[completionAcknowledgementMessage.MessageType].ReliabilityLevel == ReliabilityLevel.Persisted)
                {
                    SendToSelfShadows(completionAcknowledgementMessage.MessageId, completionAcknowledgementMessage.ProcessingSuccessful,
                        disruptorData.MessageTargetHandlerData.TargetPeer, completionAcknowledgementMessage.TransportType, completionAcknowledgementMessage.MessageType, disruptorData);

                    SendShadowMessages(concernedSubscriptions, messageData, disruptorData);

                }
            }
            else
            {
                if (_messageOptions[message.GetType().FullName].ReliabilityLevel == ReliabilityLevel.Persisted)
                    SendShadowMessages(concernedSubscriptions, messageData, disruptorData);
            }



        }

        private MessageWireData CreateMessageWireData(IMessage message)
        {
            var serializedMessage = BusSerializer.Serialize(message);
            var messageId = Guid.NewGuid();
            var messageType = message.GetType().FullName;
            var messageData = new MessageWireData(messageType, messageId, _peerConfiguration.PeerName, serializedMessage);
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


                    foreach (var peerShadow in targetShadows)
                    {
                        var endpoint = shadowSubscriptions.Single(x => x.Peer == peerShadow).Endpoint;
                        var shadowMessage = new ShadowMessageCommand(messageData, peer, true, endpoint);
                        var shadowMessageData = CreateMessageWireData(shadowMessage);
                        var wireMessage = new WireSendingMessage(shadowMessageData, endpoint);
                        disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
                    }
                }
            }
        }

        private void SendToSelfShadows(Guid messageId, bool processSuccessful, string originatingPeer, WireTransportType transportType, string originalMessageType, OutboundDisruptorEntry data)
        {
            var message = new ShadowCompletionMessage(messageId,
                                                      originatingPeer,
                                                      _peerConfiguration.PeerName,
                                                      processSuccessful, transportType, originalMessageType);
            foreach (var selfShadow in _selfShadows ?? Enumerable.Empty<string>())
            {
                var messageType = data.MessageTargetHandlerData.Message.GetType().FullName;
                var subscription = _messageTypesToSubscriptions[messageType].Single(x => x.Peer == selfShadow);
                var messageData = CreateMessageWireData(message);

                var wireMessage = new WireSendingMessage(messageData, subscription.Endpoint);
                data.NetworkSenderData.WireMessages.Add(wireMessage);
            }
        }
    }
}