using System.Collections.Generic;
using System.Configuration;
using Bus.Transport;

namespace Bus
{
    public interface IPeerConfiguration
    {
        string PeerName { get; }
        PeerId PeerId { get; }
        List<ShadowedPeerConfiguration> ShadowedPeers { get; }
    }

    class PeerConfiguration : IPeerConfiguration
    {
        public string PeerName
        {
            get { return ConfigurationManager.AppSettings["ServiceName"]; }
        }

        public PeerId PeerId { get; private set; }

        public List<ShadowedPeerConfiguration> ShadowedPeers { get; private set; }
    }
}