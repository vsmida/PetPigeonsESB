using System.Collections.Generic;
using System.Configuration;
using Bus.Transport;

namespace Bus
{
    public interface IPeerConfiguration
    {
        string PeerName { get; }
        List<ShadowedPeerConfiguration> ShadowedPeers { get; }
    }

    class PeerConfiguration : IPeerConfiguration
    {
        public string PeerName
        {
            get { return ConfigurationManager.AppSettings["ServiceName"]; }
        }

        public List<ShadowedPeerConfiguration> ShadowedPeers { get; private set; }
    }
}