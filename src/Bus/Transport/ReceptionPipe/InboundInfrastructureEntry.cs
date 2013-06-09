using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundInfrastructureEntry
    {
        public IMessage DeserializedMessage;
        public PeerId SendingPeer;
        public Guid MessageIdentity;
        public bool ServiceInitialized;
        public IEndpoint Endpoint;

    }
}