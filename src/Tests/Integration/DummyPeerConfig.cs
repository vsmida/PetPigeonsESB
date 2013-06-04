using System.Collections.Generic;
using Bus;
using Bus.Transport;

namespace Tests.Integration
{
    public class DummyPeerConfig : IPeerConfiguration
    {
        public DummyPeerConfig(string peerName, List<ShadowedPeerConfiguration> shadowedPeers)
        {
            PeerName = peerName;
            ShadowedPeers = shadowedPeers;
        }

        public string PeerName { get; private set; }
        public List<ShadowedPeerConfiguration> ShadowedPeers { get; private set; }
    }
}