using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IPeerManager
    {
        event Action<IServicePeer> PeerConnected;
        void RegisterPeer(IServicePeer peer);
        IEnumerable<IMessageSubscription> GetSubscriptionsForMessageType(string messageType);
        IMessageSubscription GetPeerSubscriptionFor(string messageType, string destinationPeer);
    }

    public class PeerManager : IPeerManager
    {
        public event Action<IServicePeer> PeerConnected = delegate { };
        private readonly ConcurrentDictionary<string, IServicePeer> _peers = new ConcurrentDictionary<string, IServicePeer>();
        private readonly ConcurrentDictionary<string, List<IMessageSubscription>> _messagesToEndpoints = new ConcurrentDictionary<string, List<IMessageSubscription>>();

        public void RegisterPeer(IServicePeer peer)
        {
            _peers.AddOrUpdate(peer.PeerName, peer, (key, oldValue) => peer);
 
            foreach (var messageToEndpoint in peer.HandledMessages)
            {
                _messagesToEndpoints.AddOrUpdate(messageToEndpoint.MessageType.FullName, key => new List<IMessageSubscription> { messageToEndpoint }, (key, oldValue) =>
                {
                    var list = new List<IMessageSubscription>(oldValue);
                    list.Add(messageToEndpoint);
                    return list;
                });
            }

            PeerConnected(peer);
        }

        public IEnumerable<IMessageSubscription> GetSubscriptionsForMessageType(string messageType)
        {
            List<IMessageSubscription> endpoints;
            _messagesToEndpoints.TryGetValue(messageType, out endpoints);
            return endpoints;
        }

        public IMessageSubscription GetPeerSubscriptionFor(string messageType, string destinationPeer)
        {
            IServicePeer peer;
            _peers.TryGetValue(destinationPeer, out peer);
            if (peer == null)
                return null;
            var messageSubscription = peer.HandledMessages.SingleOrDefault(x => x.MessageType.FullName == messageType);
            return messageSubscription;
        }
    }
}