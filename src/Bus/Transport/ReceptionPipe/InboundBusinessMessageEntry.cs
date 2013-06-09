using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundBusinessMessageEntry
    {
        public IMessage DeserializedMessage;
        public PeerId SendingPeer;
        public Guid MessageIdentity;
        public IEndpoint Endpoint;
    }
}