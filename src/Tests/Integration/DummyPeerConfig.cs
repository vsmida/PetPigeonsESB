﻿using System.Collections.Generic;
using Bus;

namespace Tests.Integration
{
    public class DummyPeerConfig : IPeerConfiguration
    {
        public DummyPeerConfig(string peerName, List<string> shadowedPeers)
        {
            PeerName = peerName;
            ShadowedPeers = shadowedPeers;
        }

        public string PeerName { get; private set; }
        public List<string> ShadowedPeers { get; private set; }
    }
}