using System;
using System.Collections.Generic;
using System.Linq;
using Bus.InfrastructureMessages;
using Bus.InfrastructureMessages.Shadowing;
using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using Shared;

namespace Bus.DisruptorEventHandlers
{
    class ReliabilityCoordinator : IReliabilityCoordinator
    {
        private readonly IPeerManager _peerManager;
        private readonly Dictionary<string, MessageSubscription> _selfMessageSubscriptions = new Dictionary<string, MessageSubscription>();
        private IEnumerable<ServicePeer> _selfShadows;
        private Dictionary<string, HashSet<ServicePeer>> _peersToShadows;
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly Dictionary<IEndpoint, int> _endpointToSequenceNumber = new Dictionary<IEndpoint, int>();


        public ReliabilityCoordinator(IPeerManager peerManager, IPeerConfiguration peerConfiguration)
        {
            _peerManager = peerManager;
            _peerConfiguration = peerConfiguration;
            _peerManager.PeerConnected += OnPeerChange;

        }

        private void OnPeerChange(ServicePeer obj)
        {
            _peersToShadows = _peerManager.GetAllShadows();
            _selfShadows = _peerManager.PeersThatShadowMe();
            if (obj.PeerName == _peerConfiguration.PeerName)
            {
                _selfMessageSubscriptions.Clear();
                foreach (var messageSubscription in obj.HandledMessages)
                {
                    _selfMessageSubscriptions[messageSubscription.MessageType.FullName] = messageSubscription;
                }
            }

        }


        public void EnsureReliability(OutboundDisruptorEntry disruptorEntry, IMessage message, MessageSubscription[] concernedSubscriptions, MessageWireData messageData)
        {
            var messageOptions = _selfMessageSubscriptions[message.GetType().FullName];

            if (messageOptions.ReliabilityLevel != ReliabilityLevel.FireAndForget)
                foreach (var wireMessage in disruptorEntry.NetworkSenderData.WireMessages)
                {
                    int seqNum;
                    if (!_endpointToSequenceNumber.TryGetValue(wireMessage.Endpoint, out seqNum))
                    {
                        _endpointToSequenceNumber.Add(wireMessage.Endpoint, 0);
                        seqNum = 0;
                    }
                    wireMessage.MessageData.SequenceNumber = seqNum;
                    _endpointToSequenceNumber[wireMessage.Endpoint] = seqNum + 1;
                }

            if (disruptorEntry.MessageTargetHandlerData.IsAcknowledgement)
            {
                SendAcknowledgementShadowMessages(message, concernedSubscriptions, disruptorEntry, messageData);
            }
            else
            {
                if (messageOptions.ReliabilityLevel == ReliabilityLevel.Persisted)
                {
                    SendShadowMessages(concernedSubscriptions, messageData, disruptorEntry);

                }
            }
        }

        private void SendAcknowledgementShadowMessages(IMessage message, MessageSubscription[] concernedSubscriptions, OutboundDisruptorEntry disruptorData, MessageWireData messageData)
        {
            var completionAcknowledgementMessage = (CompletionAcknowledgementMessage)message;
            if (_selfMessageSubscriptions[completionAcknowledgementMessage.MessageType].ReliabilityLevel == ReliabilityLevel.Persisted)
            {
                SendToSelfShadows(completionAcknowledgementMessage.MessageId,
                                  completionAcknowledgementMessage.ProcessingSuccessful,
                                  disruptorData.MessageTargetHandlerData.TargetPeer,
                                  completionAcknowledgementMessage.Endpoint,
                                  completionAcknowledgementMessage.MessageType,
                                  disruptorData);

                SendShadowMessages(concernedSubscriptions, messageData, disruptorData);
            }
        }

        private void SendShadowMessages(IEnumerable<MessageSubscription> concernedSubscriptions, MessageWireData messageData, OutboundDisruptorEntry disruptorData)
        {
            foreach (var subscription in concernedSubscriptions)
            {
                HashSet<ServicePeer> targetShadows;
                if (_peersToShadows.TryGetValue(subscription.Peer, out targetShadows))
                {
                    var endpoints = targetShadows.Select(x => x.HandledMessages.Single(y => y.MessageType == typeof(ShadowMessageCommand)).Endpoint).Distinct();

                    foreach (var endpoint in endpoints)
                    {
                        var shadowMessage = new ShadowMessageCommand(messageData, subscription.Peer, true, subscription.Endpoint);
                        var shadowMessageData = CreateMessageWireData(shadowMessage);
                        var wireMessage = new WireSendingMessage(shadowMessageData, endpoint);
                        disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
                    }
                }
            }
        }

        private void SendToSelfShadows(Guid messageId, bool processSuccessful, string originatingPeer, IEndpoint originalEndpoint, string originalMessageType, OutboundDisruptorEntry data)
        {
            var selfShadows = _selfShadows ?? Enumerable.Empty<ServicePeer>();
            if (selfShadows.Any())
            {
                var message = new ShadowCompletionMessage(messageId, originatingPeer, _peerConfiguration.PeerName, processSuccessful, originalEndpoint, originalMessageType);
                var endpoints = selfShadows.Select(x => x.HandledMessages.Single(y => y.MessageType == typeof(ShadowCompletionMessage)).Endpoint).Distinct();
                foreach (var shadowEndpoint in endpoints)
                {
                    var messageData = CreateMessageWireData(message);

                    var wireMessage = new WireSendingMessage(messageData, shadowEndpoint);
                    data.NetworkSenderData.WireMessages.Add(wireMessage);
                }
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
    }
}