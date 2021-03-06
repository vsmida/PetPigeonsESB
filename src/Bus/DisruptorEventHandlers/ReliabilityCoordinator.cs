﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bus.Dispatch;
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
        private Dictionary<string, MessageOptions> _messageOptions = new Dictionary<string, MessageOptions>();
        private IEnumerable<ServicePeer> _selfShadows;
        private Dictionary<PeerId, HashSet<ServicePeer>> _peersToShadows;
        private readonly IPeerConfiguration _peerConfiguration;
        private readonly Dictionary<IEndpoint, int> _endpointToSequenceNumber = new Dictionary<IEndpoint, int>();
        private readonly IAssemblyScanner _assemblyScanner;

        public ReliabilityCoordinator(IPeerManager peerManager, IPeerConfiguration peerConfiguration, IAssemblyScanner assemblyScanner)
        {
            _peerManager = peerManager;
            _peerConfiguration = peerConfiguration;
            _assemblyScanner = assemblyScanner;
            _peerManager.PeerConnected += OnPeerChange;

            var messageOptionses = _assemblyScanner.GetMessageOptions();
            foreach (var messageOptionse in messageOptionses)
            {
                _messageOptions.Add(messageOptionse.MessageType.FullName, messageOptionse);
            }
        }

        private void OnPeerChange(ServicePeer obj)
        {
            _peersToShadows = _peerManager.GetAllShadows().ToDictionary(x => x.Key, x => new HashSet<ServicePeer>(x.Value.Select(y => y.ServicePeer)));
            _selfShadows = _peerManager.PeersThatShadowMe().Select(x => x.ServicePeer).ToList();
            //if (obj.PeerName == _peerConfiguration.PeerName)
            //{
            //    Dictionary<string, MessageSubscription> newSelfMessageSubscriptions = new Dictionary<string, MessageSubscription>();
            //    foreach (var messageSubscription in obj.HandledMessages)
            //    {
            //        newSelfMessageSubscriptions[messageSubscription.MessageType.FullName] = messageSubscription;
            //    }
            //    _messageOptions = newSelfMessageSubscriptions;
            //}

        }


        public void EnsureReliability(OutboundDisruptorEntry disruptorEntry, IMessage message, IEnumerable<MessageSubscription> concernedSubscriptions, MessageWireData messageData)
        {
            var messageOptions = _messageOptions[message.GetType().FullName];

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

        private void SendAcknowledgementShadowMessages(IMessage message, IEnumerable<MessageSubscription> concernedSubscriptions, OutboundDisruptorEntry disruptorData, MessageWireData messageData)
        {
            var completionAcknowledgementMessage = (CompletionAcknowledgementMessage)message;
            if (_messageOptions[completionAcknowledgementMessage.MessageType].ReliabilityLevel == ReliabilityLevel.Persisted)
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

        private void SendToSelfShadows(Guid messageId, bool processSuccessful, PeerId originatingPeer, IEndpoint originalEndpoint, string originalMessageType, OutboundDisruptorEntry data)
        {
            var selfShadows = _selfShadows ?? Enumerable.Empty<ServicePeer>();
            if (selfShadows.Any())
            {
                var message = new ShadowCompletionMessage(messageId, originatingPeer, _peerConfiguration.PeerId, processSuccessful, originalEndpoint, originalMessageType);
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
            var messageData = new MessageWireData(messageType, messageId, _peerConfiguration.PeerId, serializedMessage);
            return messageData;
        }
    }
}