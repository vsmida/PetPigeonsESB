using System;
using Bus.MessageInterfaces;
using Bus.Transport.Network;

namespace Bus.Transport.ReceptionPipe
{
    public class InboundInfrastructureEntry
    {
        public IMessage DeserializedMessage;
        public string SendingPeer;
        public Guid MessageIdentity;
        public bool ServiceInitialized;
        public IEndpoint Endpoint;

    }
}