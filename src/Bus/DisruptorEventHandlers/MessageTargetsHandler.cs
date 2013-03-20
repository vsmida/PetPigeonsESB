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
            var allSubscriptions = _messageTypesToSubscriptions[messageType];
            var concernedSubscriptions = allSubscriptions
              .Where(x => (x.SubscriptionFilter == null || x.SubscriptionFilter.Matches(data.MessageTargetHandlerData.Message))
                     && (data.MessageTargetHandlerData.TargetPeer == null || x.Peer == data.MessageTargetHandlerData.TargetPeer)).ToArray();

            var messageData = CreateMessageWireData(data.MessageTargetHandlerData.Message);
            var callback = data.MessageTargetHandlerData.Callback;
            if (callback != null)
                _callbackRepository.RegisterCallback(messageData.MessageIdentity, callback);

            for (int i = 0; i < allSubscriptions.Count; i++)
            {
                var subscription = allSubscriptions[i];
                var endpoint = subscription.Endpoint;
                if ((subscription.SubscriptionFilter == null || subscription.SubscriptionFilter.Matches(data.MessageTargetHandlerData.Message))
                    && (data.MessageTargetHandlerData.TargetPeer == null || subscription.Peer == data.MessageTargetHandlerData.TargetPeer)
                    && !data.NetworkSenderData.WireMessages.Any(x => Equals(x.Endpoint, endpoint)))
                {
                    var wireMessage = new WireSendingMessage(messageData, endpoint);
                    data.NetworkSenderData.WireMessages.Add(wireMessage);
                }
                    
            }

         //   SendToConcernedPeers(concernedSubscriptions, data, messageData);

            _reliabilityCoordinator.EnsureReliability(data, data.MessageTargetHandlerData.Message, concernedSubscriptions, messageData);

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