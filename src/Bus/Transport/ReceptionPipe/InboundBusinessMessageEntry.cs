using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundBusinessMessageEntry
    {
        public IMessage DeserializedMessage;
        public string SendingPeer;
        public Guid MessageIdentity;
        public WireTransportType TransportType;
    }
}