using System;
using ZmqServiceBus.Bus.MessageInterfaces;

namespace ZmqServiceBus.Bus.Transport.ReceptionPipe
{
    public class InboundInfrastructureEntry
    {
        public IMessage DeserializedMessage;
        public string SendingPeer;
        public Guid MessageIdentity;
        public bool ServiceInitialized;
    }
}