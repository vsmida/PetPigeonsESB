using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Bus.Transport.Network
{

    public class ServicePeerShadowInformation
    {
        public readonly ServicePeer ServicePeer;
        public readonly bool IsPersistenceProvider;

        public ServicePeerShadowInformation(ServicePeer servicePeer, bool isPersistenceProvider)
        {
            ServicePeer = servicePeer;
            IsPersistenceProvider = isPersistenceProvider;
        }


    }

    public interface IPeerManager
    {
        event Action<ServicePeer> PeerConnected;
        event Action<EndpointStatus> EndpointStatusUpdated;
        void RegisterPeerConnection(ServicePeer peer);
        List<ServicePeer> GetAllPeers();
        IEnumerable<ServicePeerShadowInformation> PeersThatShadowMe();
        Dictionary<string, List<MessageSubscription>> GetAllSubscriptions();
        Dictionary<PeerId, HashSet<ServicePeerShadowInformation>> GetAllShadows();
        string Self { get; }
        Dictionary<IEndpoint, EndpointStatus> GetEndpointStatuses();
    }


    public class PeerManager : IPeerManager
    {
        public event Action<ServicePeer> PeerConnected = delegate { };
        public event Action<EndpointStatus> EndpointStatusUpdated = delegate { };
        private readonly ConcurrentDictionary<PeerId, ServicePeer> _peers = new ConcurrentDictionary<PeerId, ServicePeer>();
        private readonly ConcurrentDictionary<string, List<MessageSubscription>> _messagesToEndpoints = new ConcurrentDictionary<string, List<MessageSubscription>>();
        private readonly ConcurrentDictionary<IEndpoint, EndpointStatus> _endpointToStatus = new ConcurrentDictionary<IEndpoint, EndpointStatus>();
        private readonly ConcurrentDictionary<PeerId, HashSet<ServicePeerShadowInformation>> _peersToTheirShadows = new ConcurrentDictionary<PeerId, HashSet<ServicePeerShadowInformation>>();
        private readonly IPeerConfiguration _peerConfig;
        private readonly IHeartbeatManager _heartbeatManager;

        public PeerManager(IPeerConfiguration peerConfig, IHeartbeatManager heartbeatManager)
        {
            _peerConfig = peerConfig;
            _heartbeatManager = heartbeatManager;
            _heartbeatManager.Disconnected += OnHeartbeatManagerDisconnected;
        }

        private void OnHeartbeatManagerDisconnected(IEndpoint endpoint)
        {
            _endpointToStatus[endpoint].Connected = false;
            EndpointStatusUpdated(_endpointToStatus[endpoint]);
        }


        public void RegisterPeerConnection(ServicePeer peer)
        {
            UpdatePeerList(peer);

            UpdateSubscriptions(peer);

            UpdateShadows(peer);

            PeerConnected(peer);

            foreach (var subscription in peer.HandledMessages)
            {
                _endpointToStatus[subscription.Endpoint] = new EndpointStatus(subscription.Endpoint, true);
            }
        }

        private void UpdatePeerList(ServicePeer peer)
        {
            _peers.AddOrUpdate(peer.PeerId, peer, (key, oldValue) => { return peer; });
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
                                                             oldValue.Where(x => x.Peer != peer.PeerId));
                                                     //dont keep previous message subscription from peer
                                                     list.Add(messageToEndpoint);
                                                     return list;
                                                 });
                
            }

            foreach (var pair in _messagesToEndpoints) //remove messages that are no longer handled
            {
                if (peer.HandledMessages.All(x => x.MessageType.FullName != pair.Key))
                    pair.Value.RemoveAll(x => x.Peer == peer.PeerId);
            }
        }

        private void UpdateShadows(ServicePeer peer)
        {
            foreach (var shadowedPeer in peer.ShadowedPeers ?? Enumerable.Empty<ShadowedPeerConfiguration>())
            {
                _peersToTheirShadows.AddOrUpdate(shadowedPeer.PeerPeerId,
                                            new HashSet<ServicePeerShadowInformation> { new ServicePeerShadowInformation(peer, shadowedPeer.IsPersistenceProvider) },
                                            (key, oldValue) =>
                                            {
                                                oldValue.Add(new ServicePeerShadowInformation(peer, shadowedPeer.IsPersistenceProvider));
                                                return oldValue;
                                            });
            }

            foreach (var pair in _peersToTheirShadows)
            {
                var previousEntry = pair.Value.SingleOrDefault(x => x.ServicePeer == peer);
                if (previousEntry != null && !peer.ShadowedPeers.Select(x => x.PeerPeerId).Contains(pair.Key))
                    pair.Value.Remove(previousEntry);
            }
        }


        public IEnumerable<ServicePeerShadowInformation> PeersThatShadowMe()
        {
            HashSet<ServicePeerShadowInformation> shadows;
            _peersToTheirShadows.TryGetValue(_peerConfig.PeerId, out shadows);
            return shadows ?? Enumerable.Empty<ServicePeerShadowInformation>();
        }

        public Dictionary<string, List<MessageSubscription>> GetAllSubscriptions()
        {
            return _messagesToEndpoints.ToDictionary(x => x.Key, x => x.Value);
        }

        public Dictionary<PeerId, HashSet<ServicePeerShadowInformation>> GetAllShadows()
        {
            return _peersToTheirShadows.ToDictionary(x => x.Key, x => x.Value);
        }

        public string Self
        {
            get { return _peerConfig.PeerName; }
        }

        public Dictionary<IEndpoint, EndpointStatus> GetEndpointStatuses()
        {
            return _endpointToStatus.ToDictionary(x => x.Key, x => x.Value);
        }

        public List<ServicePeer> GetAllPeers()
        {
            return _peers.Values.ToList();
        }

    }
}