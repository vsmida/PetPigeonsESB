using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Shared;

namespace ZmqServiceBus.Bus.Transport
{
    public class EndpointManager : IEndpointManager
    {
        private class SocketInfo
        {
            public BlockingCollection<ISendingTransportMessage> SendingQueue { get; set; }
            public bool SocketInitialized { get; set; }

            public SocketInfo()
            {
                SendingQueue = new BlockingCollection<ISendingTransportMessage>();
            }
        }

        private readonly Dictionary<string, SocketInfo> _endpointsToSocketInfo = new Dictionary<string, SocketInfo>();
        private readonly Dictionary<string, HashSet<string>> _messageTypesToEndpoints = new Dictionary<string, HashSet<string>>();
        private readonly BlockingCollection<ISendingTransportMessage> _messagesToPublish = new BlockingCollection<ISendingTransportMessage>();
        private readonly BlockingCollection<IReceivedTransportMessage> _messagesToForward = new BlockingCollection<IReceivedTransportMessage>();
        private readonly Dictionary<string, IServicePeer> _knownPeersById = new Dictionary<string, IServicePeer>();
        private readonly HashSet<Type> listenedToEvents = new HashSet<Type>();
        private readonly TransportConfiguration _configuration;
        private readonly IZmqSocketManager _socketManager;
        private volatile bool _running = true;

        public event Action<IReceivedTransportMessage> OnMessageReceived = delegate { };


        public EndpointManager(TransportConfiguration configuration, IZmqSocketManager socketManager)
        {
            _configuration = configuration;
            _socketManager = socketManager;
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
            HashSet<string> endpoints = _messageTypesToEndpoints[message.MessageType];
            foreach (var endpoint in endpoints)
            {
                var socketInfo = _endpointsToSocketInfo[endpoint];
                if (!socketInfo.SocketInitialized)
                {
                    _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, endpoint, _configuration.PeerName);
                    socketInfo.SocketInitialized = true;
                }
                socketInfo.SendingQueue.Add(message);
            }

        }

        public void PublishMessage(ISendingTransportMessage message)
        {
            _messagesToPublish.Add(message);
        }

        public void RouteMessage(ISendingTransportMessage message, string destinationPeer)
        {
            IServicePeer peer;
            if (!_knownPeersById.TryGetValue(destinationPeer, out peer))
                throw new ArgumentException(string.Format("Cannot route to an unknown peer {0}", destinationPeer));
            var targetEndpoint = peer.ReceptionEndpoint;
            var socketInfo = _endpointsToSocketInfo[targetEndpoint];
            if (!socketInfo.SocketInitialized)
            {
                _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, targetEndpoint, _configuration.PeerName);
                socketInfo.SocketInitialized = true;
            }
            socketInfo.SendingQueue.Add(message);
        }

        public void RegisterPeer(IServicePeer peer)
        {
            _knownPeersById[peer.PeerName] = peer;
            foreach (var publishedMessageType in peer.PublishedMessages)
            {
                if (listenedToEvents.Contains(publishedMessageType))
                    _socketManager.SubscribeTo(peer.PublicationEndpoint, publishedMessageType.FullName);
            }

            foreach (var handledMessage in peer.HandledMessages)
            {
                RegisterPeerEnpointForMessageType(peer, handledMessage);
            }
        }

        private void RegisterPeerEnpointForMessageType(IServicePeer peer, Type handledMessage)
        {
            HashSet<string> endpointsForMessageType;
            if (!_messageTypesToEndpoints.TryGetValue(handledMessage.FullName, out endpointsForMessageType))
            {
                endpointsForMessageType = new HashSet<string>();
                _messageTypesToEndpoints[handledMessage.FullName] = endpointsForMessageType;
            }
            endpointsForMessageType.Add(peer.ReceptionEndpoint);
            if (!_endpointsToSocketInfo.ContainsKey(peer.ReceptionEndpoint))
            {
                _endpointsToSocketInfo[peer.ReceptionEndpoint] = new SocketInfo();
            }
        }

        public void Dispose()
        {
            _running = false;
            _socketManager.Stop();
        }

        public void ListenTo<T>()
        {
            listenedToEvents.Add(typeof(T));
            foreach (var servicePeer in _knownPeersById.Values)
            {
                if (servicePeer.PublishedMessages.Contains(typeof(T)))
                    _socketManager.SubscribeTo(servicePeer.PublicationEndpoint, typeof(T).FullName);
            }
        }
    }
}