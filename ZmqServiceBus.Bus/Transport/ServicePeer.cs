using System;
using System.Collections.Generic;
using ProtoBuf;
using ZmqServiceBus.Bus.Transport.Network;

namespace ZmqServiceBus.Bus.Transport
{


    public interface IServicePeer
    {
        string PeerName { get; }
        IEnumerable<IMessageSubscription> HandledMessages { get; }
    }

    [ProtoContract]
    public class ServicePeer : IServicePeer
    {
        [ProtoMember(1, IsRequired = true)]
        public string PeerName { get; private set; }
        [ProtoMember(4, IsRequired = true)]
        private IEnumerable<IMessageSubscription> _handledMessages;
        public IEnumerable<IMessageSubscription> HandledMessages
        {
            get { return _handledMessages; }
        }

        public ServicePeer(string peerName, IEnumerable<IMessageSubscription> handledMessages)
        {
            PeerName = peerName;
            _handledMessages = handledMessages;
        }

        private ServicePeer()
        {}
    }
}