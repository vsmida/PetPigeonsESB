using System;
using System.Collections.Generic;
using ProtoBuf;
using ZmqServiceBus.Bus.Transport.Network;
using System.Linq;

namespace ZmqServiceBus.Bus.Transport
{

    [ProtoContract]
    public class ServicePeer
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName { get; private set; }
        [ProtoMember(2, IsRequired = true)]
        private List<MessageSubscription> _handledMessages;
        public IEnumerable<IMessageSubscription> HandledMessages
        {
            get { return _handledMessages; }
        }

        public ServicePeer(string peerName, IEnumerable<MessageSubscription> handledMessages)
        {
            PeerName = peerName;
            _handledMessages = handledMessages.ToList();
        }

        private ServicePeer()
        {}
    }
}