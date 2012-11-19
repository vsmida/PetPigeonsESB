using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Shared;

namespace ZmqServiceBus.Bus.Transport.Network
{
    public interface IPeerManager
    {
        event Action<IServicePeer> PeerConnected;
        void RegisterPeer(IServicePeer peer);
        IEnumerable<string> GetEndpointsForMessageType(string messageType);
        string GetPeerEndpointFor(string messageType, string destinationPeer);
    }

    public class PeerManager : IPeerManager
    {
        public event Action<IServicePeer> PeerConnected = delegate { };
        private readonly ConcurrentDictionary<string, IServicePeer> _peers = new ConcurrentDictionary<string, IServicePeer>();
        private readonly ConcurrentDictionary<string, List<string>> _eventsToEndpoints = new ConcurrentDictionary<string, List<string>>();
        private readonly ConcurrentDictionary<string, List<string>> _commandsToEndpoints = new ConcurrentDictionary<string, List<string>>();

        public void RegisterPeer(IServicePeer peer)
        {
            _peers.AddOrUpdate(peer.PeerName, peer, (key, oldValue) => peer);
            foreach (var ev in peer.PublishedMessages)
            {
                _eventsToEndpoints.AddOrUpdate(ev.FullName, key => new List<string> { peer.PublicationEndpoint }, (key, oldValue) =>
                                                                                         {
                                                                                             var list = new List<string>(oldValue);
                                                                                             list.Add(peer.PublicationEndpoint);
                                                                                             return list;
                                                                                         });
            }
            foreach (var command in peer.HandledMessages)
            {
                _commandsToEndpoints.AddOrUpdate(command.FullName, key => new List<string> { peer.ReceptionEndpoint }, (key, oldValue) =>
                {
                    var list = new List<string>(oldValue);
                    list.Add(peer.ReceptionEndpoint);
                    return list;
                });
            }

            PeerConnected(peer);
        }

        public IEnumerable<string> GetEndpointsForMessageType(string messageType)
        {
            List<string> endpoints;
            _eventsToEndpoints.TryGetValue(messageType, out endpoints);
            if (endpoints != null)
                return endpoints;
            _commandsToEndpoints.TryGetValue(messageType, out endpoints);
            return endpoints;
        }

        public string GetPeerEndpointFor(string messageType, string destinationPeer)
        {
            IServicePeer peer;
            _peers.TryGetValue(destinationPeer, out peer);
            if (peer.PublishedMessages.Any(x => x.FullName == messageType))
                return peer.PublicationEndpoint;
            return peer.ReceptionEndpoint;
        }
    }
}