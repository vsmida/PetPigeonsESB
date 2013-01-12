using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Shared;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IPeerManager
    {
        event Action<ServicePeer> PeerConnected;
        void RegisterPeerConnection(ServicePeer peer);
        IEnumerable<MessageSubscription> GetSubscriptionsForMessageType(string messageType);
        MessageSubscription GetPeerSubscriptionFor(string messageType, string destinationPeer);
        List<ServicePeer> GetAllPeers();
        IEnumerable<string> PeersThatShadowMe();
        Dictionary<string, List<MessageSubscription>> GetAllSubscriptions();
        Dictionary<string, HashSet<string>> GetAllShadows();
        string Self { get; }
    }


    public class PeerManager : IPeerManager
    {
        public event Action<ServicePeer> PeerConnected = delegate { };
        private readonly ConcurrentDictionary<string, ServicePeer> _peers = new ConcurrentDictionary<string, ServicePeer>();
        private readonly ConcurrentDictionary<string, List<MessageSubscription>> _messagesToEndpoints = new ConcurrentDictionary<string, List<MessageSubscription>>();
        private readonly ConcurrentDictionary<string, HashSet<string>> _peersToShadows = new ConcurrentDictionary<string, HashSet<string>>();
        private readonly IPeerConfiguration _peerConfig;

        public PeerManager(IPeerConfiguration peerConfig)
        {
            _peerConfig = peerConfig;
        }


        public void RegisterPeerConnection(ServicePeer peer)
        {
            UpdatePeerList(peer);

            UpdateSubscriptions(peer);

            UpdateShadows(peer);

            PeerConnected(peer);
        }

        private void UpdatePeerList(ServicePeer peer)
        {
            _peers.AddOrUpdate(peer.PeerName, peer, (key, oldValue) => { return peer; });
        }

        private void UpdateSubscriptions(ServicePeer peer)
        {
            foreach (var messageToEndpoint in peer.HandledMessages)
            {
                _messagesToEndpoints.AddOrUpdate(messageToEndpoint.MessageType.FullName,
                                                 key => new List<MessageSubscription> { messageToEndpoint },
                                                 (key, oldValue) =>
                                                 {
                                                     var list =
                                                         new List<MessageSubscription>(
                                                             oldValue.Where(x => x.Peer != peer.PeerName));
                                                     //dont keep previous message subscription from peer
                                                     list.Add(messageToEndpoint);
                                                     return list;
                                                 });
            }

            foreach (var pair in _messagesToEndpoints) //remove messages that are no longer handled
            {
                if (peer.HandledMessages.All(x => x.MessageType.FullName != pair.Key))
                    pair.Value.RemoveAll(x => x.Peer == peer.PeerName);
            }
        }

        private void UpdateShadows(ServicePeer peer)
        {
            foreach (var shadowedPeer in peer.ShadowedPeers ?? Enumerable.Empty<string>())
            {
                _peersToShadows.AddOrUpdate(shadowedPeer,
                                            new HashSet<string> { peer.PeerName },
                                            (key, oldValue) =>
                                            {
                                                oldValue.Add(peer.PeerName);
                                                return oldValue;
                                            });
            }

            foreach (var pair in _peersToShadows)
            {
                if (pair.Value.Contains(peer.PeerName) && !peer.ShadowedPeers.Contains(pair.Key))
                    pair.Value.Remove(peer.PeerName);
            }
        }


        public IEnumerable<string> PeersThatShadowMe()
        {
            HashSet<string> shadows;
            _peersToShadows.TryGetValue(_peerConfig.PeerName, out shadows);
            return shadows;
        }

        public Dictionary<string, List<MessageSubscription>> GetAllSubscriptions()
        {
            return _messagesToEndpoints.ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<string, HashSet<string>> GetAllShadows()
        {
            return _peersToShadows.ToDictionary(x => x.Key, x => x.Value);
        }

        public string Self
        {
            get { return _peerConfig.PeerName; }
        }

        public IEnumerable<MessageSubscription> GetSubscriptionsForMessageType(string messageType)
        {
            List<MessageSubscription> endpoints;
            _messagesToEndpoints.TryGetValue(messageType, out endpoints);
            return endpoints;
        }

        public MessageSubscription GetPeerSubscriptionFor(string messageType, string destinationPeer)
        {
            ServicePeer peer;
            _peers.TryGetValue(destinationPeer, out peer);
            if (peer == null)
                return null;
            var messageSubscription = peer.HandledMessages.SingleOrDefault(x => x.MessageType.FullName == messageType);
            return messageSubscription;
        }

        public List<ServicePeer> GetAllPeers()
        {
            return _peers.Values.ToList();
        }

    }
}