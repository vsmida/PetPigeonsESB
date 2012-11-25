using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;
using ZmqServiceBus.Bus.Transport.ReceptionPipe;
using ZmqServiceBus.Bus.Transport.SendingPipe;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public class EndpointManager : IEndpointManager
    {
        private class SocketInfo
        {
            public BlockingCollection<ISendingTransportMessage> SendingQueue { get; set; }

            public SocketInfo()
            {
                SendingQueue = new BlockingCollection<ISendingTransportMessage>();
            }
        }

        private readonly IPeerManager _peerManager;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly BlockingCollection<ISendingTransportMessage> _messagesToPublish = new BlockingCollection<ISendingTransportMessage>();
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();
        private readonly Dictionary<string, SocketInfo> _endpointsToSocketInfo = new Dictionary<string, SocketInfo>();


        private readonly TransportConfiguration _configuration;
        private readonly IZmqSocketManager _socketManager;
        private volatile bool _running = true;

        public event Action<IReceivedTransportMessage> OnMessageReceived = delegate { };


        public EndpointManager(TransportConfiguration configuration, IZmqSocketManager socketManager, IPeerManager peerManager, ISubscriptionManager subscriptionManager)
        {
            _configuration = configuration;
            _socketManager = socketManager;
            _peerManager = peerManager;
            _subscriptionManager = subscriptionManager;
            _subscriptionManager.NewEventSubscription += NewSubscription;
        }

        private void NewSubscription(Type eventType)
        {
            var publishingEndpoints = _peerManager.GetEndpointsForMessageType(eventType.FullName);
            foreach (var publishingEndpoint in publishingEndpoints)
            {
                _socketManager.SubscribeTo(publishingEndpoint, eventType.FullName);
            }
        }

        public void Initialize()
        {
            _socketManager.CreateResponseSocket(_messagesToForward, _configuration.GetCommandsEnpoint(), _configuration.PeerName);
            _socketManager.CreatePublisherSocket(_messagesToPublish, _configuration.GetEventsEndpoint(), _configuration.PeerName);
            _socketManager.CreateSubscribeSocket(_messagesToForward);
            CreateTransportMessageProcessingThread();
        }

        private void CreateTransportMessageProcessingThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             IReceivedTransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                                 OnMessageReceived(message);
                                         }
                                     }).Start();
        }

        public void SendMessage(ISendingTransportMessage message)
        {
            var endpoints = _peerManager.GetEndpointsForMessageType(message.MessageType);
            foreach (var endpoint in endpoints)
            {
                var socketInfo = GetOrCreateSocketInfo(endpoint);
                socketInfo.SendingQueue.Add(message);
            }

        }

        public void PublishMessage(ISendingTransportMessage message)
        {
            _messagesToPublish.Add(message);
        }

        public void RouteMessage(ISendingTransportMessage message, string destinationPeer)
        {
            string endpoint = _peerManager.GetPeerEndpointFor(message.MessageType, destinationPeer);
            var socketInfo = GetOrCreateSocketInfo(endpoint);
            socketInfo.SendingQueue.Add(message);
        }

        private SocketInfo GetOrCreateSocketInfo(string endpoint)
        {
            SocketInfo socketInfo;
            if (!_endpointsToSocketInfo.TryGetValue(endpoint, out socketInfo))
            {
                socketInfo = new SocketInfo();
                _endpointsToSocketInfo[endpoint] = socketInfo;
                _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, endpoint,
                                                   _configuration.PeerName);
            }
            return socketInfo;
        }


        public void Dispose()
        {
            _running = false;
            _socketManager.Stop();
        }

    }
}