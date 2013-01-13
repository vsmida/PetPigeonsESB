using System.Collections.Generic;
using ProtoBuf;

namespace Bus.Transport
{

    [ProtoContract]
    public class ServicePeer
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        public readonly List<MessageSubscription> HandledMessages;
        [ProtoMember(3, IsRequired = true)]
        public readonly List<string> ShadowedPeers;

        public ServicePeer(string peerName, List<MessageSubscription> handledMessages, List<string> shadowedPeers)
        {
            PeerName = peerName;
            HandledMessages = handledMessages;
            ShadowedPeers = shadowedPeers;
        }

        private ServicePeer()
        {
        }
    }
}