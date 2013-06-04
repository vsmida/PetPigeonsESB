using System.Collections.Generic;
using ProtoBuf;

namespace Bus.Transport
{
    [ProtoContract]
    public class ShadowedPeerConfiguration
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly string PeerName;
        [ProtoMember(2, IsRequired = true)]
        public readonly bool IsPersistenceProvider;

        public ShadowedPeerConfiguration(string name, bool isPersistenceProvider)
        {
            PeerName = name;
            IsPersistenceProvider = isPersistenceProvider;
        }

        private ShadowedPeerConfiguration()
        {
            
        }
    }

    [ProtoContract]
    public class ServicePeer
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public readonly List<MessageSubscription> HandledMessages;
        [ProtoMember(3, IsRequired = true)]
        public readonly List<ShadowedPeerConfiguration> ShadowedPeers;

        public ServicePeer(string peerName, List<MessageSubscription> handledMessages, List<ShadowedPeerConfiguration> shadowedPeers)
        {
            PeerName = peerName;
            HandledMessages = handledMessages;
            ShadowedPeers = shadowedPeers;
        }

        private ServicePeer()
        {
        }

        protected bool Equals(ServicePeer other)
        {
            return string.Equals(PeerName, other.PeerName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServicePeer)obj);
        }

        public override int GetHashCode()
        {
            return (PeerName != null ? PeerName.GetHashCode() : 0);
        }
    }
}