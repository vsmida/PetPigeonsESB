using System;
using System.Collections.Generic;
using System.Linq;
using Bus.MessageInterfaces;
using Bus.Transport;
using Bus.Transport.Network;
using Bus.Transport.SendingPipe;
using Disruptor;

namespace Bus.DisruptorEventHandlers
{
    class MessageTargetsHandler : IEventHandler<OutboundDisruptorEntry>
    {

        private readonly ICallbackRepository _callbackRepository;
        private readonly IPeerManager _peerManager;
        private readonly IPeerConfiguration _peerConfiguration;

        private Dictionary<string, List<MessageSubscription>> _messageTypesToSubscriptions;
        private readonly IReliabilityCoordinator _reliabilityCoordinator;


        public MessageTargetsHandler(ICallbackRepository callbackRepository, IPeerManager peerManager, IPeerConfiguration peerConfiguration, IReliabilityCoordinator reliabilityCoordinator)
        {
            _callbackRepository = callbackRepository;
            _peerManager = peerManager;
            _peerConfiguration = peerConfiguration;
            _reliabilityCoordinator = reliabilityCoordinator;
            _peerManager.PeerConnected += OnPeerChange;
            _peerManager.EndpointStatusUpdated += OnEndpointStatusUpdated;

        }

        private void OnEndpointStatusUpdated(EndpointStatus obj)
        {
            UpdateSubscriptions();
        }

        private void OnPeerChange(ServicePeer obj)
        {
            //reference assignement is atomic;
            UpdateSubscriptions();
        }

        private void UpdateSubscriptions()
        {
            var messageTypesToSubscriptions = _peerManager.GetAllSubscriptions();

            _messageTypesToSubscriptions = messageTypesToSubscriptions;
        }

        public void OnNext(OutboundDisruptorEntry data, long sequence, bool endOfBatch)
        {
            if (data.MessageTargetHandlerData.Message == null)
                return;

            var messageType = data.MessageTargetHandlerData.Message.GetType().FullName;
            var subscriptions = _messageTypesToSubscriptions[messageType].Where(x => data.MessageTargetHandlerData.TargetPeer == null || x.Peer == data.MessageTargetHandlerData.TargetPeer).ToList();
            //  .Where(x => (x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(data.MessageTargetHandlerData.Message))
            //         && (data.MessageTargetHandlerData.TargetPeer == null || x.Peer == data.MessageTargetHandlerData.TargetPeer)).ToArray();

            SendUsingSubscriptions(data.MessageTargetHandlerData.Message, data.MessageTargetHandlerData.Callback, subscriptions, data);

        }

        private void SendUsingSubscriptions(IMessage message, ICompletionCallback callback, List<MessageSubscription> concernedSubscriptions, OutboundDisruptorEntry disruptorData)
        {
            var messageData = CreateMessageWireData(message);

            if (callback != null)
                _callbackRepository.RegisterCallback(messageData.MessageIdentity, callback);

            SendToConcernedPeers(concernedSubscriptions, disruptorData, messageData);

            _reliabilityCoordinator.EnsureReliability(disruptorData, message, concernedSubscriptions, messageData);

        }

        private static void SendToConcernedPeers(List<MessageSubscription> concernedSubscriptions, OutboundDisruptorEntry disruptorData, MessageWireData messageData)
        {
            HashSet<IEndpoint> endpoints = new HashSet<IEndpoint>();
            for (int i = 0; i < concernedSubscriptions.Count; i++)
            {
                var endpoint = concernedSubscriptions[i].Endpoint;
                if (!endpoints.Contains(endpoint))
                {
                    var wireMessage = new WireSendingMessage(messageData, endpoint);
                    endpoints.Add(endpoint);
                    disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
                }
            }

            //foreach (var endpoint in concernedSubscriptions.Select(x => x.Endpoint).Distinct())
            //{
            //    var wireMessage = new WireSendingMessage(messageData, endpoint);
            //    disruptorData.NetworkSenderData.WireMessages.Add(wireMessage);
            //}
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