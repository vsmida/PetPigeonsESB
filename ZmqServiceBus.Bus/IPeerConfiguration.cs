using System.Collections.Generic;
using System.Configuration;

namespace ZmqServiceBus.Bus
{
    public interface IPeerConfiguration
    {
        string PeerName { get; }
        List<string> ShadowedPeers { get; }
    }

    class PeerConfiguration : IPeerConfiguration
    {
        public string PeerName
        {
            get { return ConfigurationManager.AppSettings["ServiceName"]; }
        }

        public List<string> ShadowedPeers { get; private set; }
    }
}