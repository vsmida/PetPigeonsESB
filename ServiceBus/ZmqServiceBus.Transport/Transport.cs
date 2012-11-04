using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using ProtoBuf.Meta;
using Shared;
using ZeroMQ;

namespace ZmqServiceBus.Transport
{
    public class Transport : ITransport
    {
        private class SocketInfo
        {
            public BlockingCollection<ITransportMessage> SendingQueue { get; set; }
            public bool SocketInitialized { get; set; }

            public SocketInfo()
            {
                SendingQueue = new BlockingCollection<ITransportMessage>();
            }
        }

        private readonly Dictionary<string, SocketInfo> _endpointsToSocketInfo = new Dictionary<string, SocketInfo>();
        private readonly Dictionary<string, HashSet<string>> _messageTypesToEndpoints = new Dictionary<string, HashSet<string>>();
        private readonly BlockingCollection<ITransportMessage> _messagesToPublish = new BlockingCollection<ITransportMessage>();
        private readonly BlockingCollection<ITransportMessage> _messagesToForward = new BlockingCollection<ITransportMessage>();
        private readonly Dictionary<string, IServicePeer> _knownPeersById = new Dictionary<string, IServicePeer>();
        private readonly HashSet<Type> listenedToEvents = new HashSet<Type>();
        private readonly HashSet<Type> commandsSent = new HashSet<Type>();
        public TransportConfiguration Configuration { get; private set; }
        private readonly IZmqSocketManager _socketManager;
        public event Action<ITransportMessage> OnMessageReceived = delegate { };
        private volatile bool _running = true;

        public Transport(TransportConfiguration configuration, IZmqSocketManager socketManager)
        {
            Configuration = configuration;
            _socketManager = socketManager;
        }


        public void Initialize()
        {
            _socketManager.CreateResponseSocket(_messagesToForward, Configuration.GetCommandsEnpoint(), Configuration.PeerName);
            _socketManager.CreatePublisherSocket(_messagesToPublish, Configuration.GetEventsEndpoint(), Configuration.PeerName);
            _socketManager.CreateSubscribeSocket(_messagesToForward);
            CreateTransportMessageProcessingThread();
        }

        private void CreateTransportMessageProcessingThread()
        {
            new BackgroundThread(() =>
                                     {
                                         while (_running)
                                         {
                                             ITransportMessage message;
                                             if (_messagesToForward.TryTake(out message, TimeSpan.FromMilliseconds(500)))
                                             {
                                                 OnMessageReceived(message);
                                             }
                                         }

                                     }).Start();
        }

        public void SendMessage(ITransportMessage message)
        {
            HashSet<string> endpoints = _messageTypesToEndpoints[message.MessageType];
            foreach (var endpoint in endpoints)
            {
                var socketInfo = _endpointsToSocketInfo[endpoint];
                if (!socketInfo.SocketInitialized)
                {
                    _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, endpoint, Configuration.PeerName);
                    socketInfo.SocketInitialized = true;
                }
                socketInfo.SendingQueue.Add(message);
            }

        }



        public void PublishMessage(ITransportMessage message)
        {
            _messagesToPublish.Add(message);
        }

        public void RouteMessage(ITransportMessage message)
        {
            IServicePeer peer;
            if (!_knownPeersById.TryGetValue(message.PeerName, out peer))
                throw new ArgumentException(string.Format("Cannot route to an unknown peer {0}", message.PeerName));
            var targetEndpoint = peer.ReceptionEndpoint;
            var socketInfo = _endpointsToSocketInfo[targetEndpoint];
            if (!socketInfo.SocketInitialized)
            {
                _socketManager.CreateRequestSocket(socketInfo.SendingQueue, _messagesToForward, targetEndpoint, Configuration.PeerName);
                socketInfo.SocketInitialized = true;
            }
            socketInfo.SendingQueue.Add(message);
        }

        public void AckMessage(byte[] recipientIdentity, Guid messageId, bool success)
        {
            //   _acknowledgementsToSend.Add(new TransportMessage(Guid.NewGuid(), typeof(AcknowledgementMessage).FullName, Serializer.Serialize(new AcknowledgementMessage(messageId, success))));
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